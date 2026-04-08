using System.Globalization;
using CsvHelper;
using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[ApiController]
[Route("api/courses/{courseId:int}/gradebook")]
[Authorize]
public class GradebookController(LmsDbContext db) : ControllerBase
{
    private string UserId => User.FindFirst("sub")?.Value ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";

    // Teacher: get all students' grades
    [HttpGet]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<IEnumerable<GradebookStudentRow>>> GetAll(int courseId)
    {
        if (!await IsTeacherOrAdmin(courseId)) return Forbid();

        var enrollments = await db.Enrollments
            .Where(e => e.CourseId == courseId)
            .Select(e => new { e.UserId, e.UserName })
            .ToListAsync();

        var assignments = await db.Assignments
            .Include(a => a.Submissions)
            .Where(a => a.CourseId == courseId)
            .ToListAsync();

        var quizzes = await db.Quizzes
            .Where(q => q.CourseId == courseId && q.IsPublished)
            .ToListAsync();

        var allAttempts = await db.QuizAttempts
            .Where(a => a.Quiz.CourseId == courseId && a.SubmittedAt != null)
            .ToListAsync();

        var rows = enrollments.Select(e =>
        {
            var assignmentItems = assignments.Select(a =>
            {
                var sub = a.Submissions.FirstOrDefault(s => s.UserId == e.UserId);
                return new GradeItem(
                    a.Id, a.Title,
                    sub?.Score, a.MaxScore,
                    sub == null ? "not_submitted" : sub.Status.ToString().ToLower()
                );
            }).ToList();

            var quizItems = quizzes.Select(q =>
            {
                var attempt = allAttempts
                    .Where(a => a.QuizId == q.Id && a.UserId == e.UserId)
                    .OrderByDescending(a => a.Score)
                    .FirstOrDefault();
                return new GradeItem(
                    q.Id, q.Title,
                    attempt?.Score,
                    attempt?.MaxScore ?? 100,
                    attempt == null ? "not_submitted" : "graded"
                );
            }).ToList();

            var allItems = assignmentItems.Cast<GradeItem>().Concat(quizItems).ToList();
            var gradedItems = allItems.Where(i => i.Score.HasValue).ToList();
            double? pct = gradedItems.Any()
                ? gradedItems.Sum(i => (double)i.Score! / i.MaxScore * 100) / allItems.Count
                : null;

            return new GradebookStudentRow(
                e.UserId, e.UserName, string.Empty,
                assignmentItems, quizItems,
                pct.HasValue ? Math.Round(pct.Value, 1) : null,
                ToLetterGrade(pct));
        }).ToList();

        return Ok(rows);
    }

    // Student: get my grades
    [HttpGet("me")]
    public async Task<ActionResult<GradebookMyView>> GetMine(int courseId)
    {
        var isEnrolled = await db.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserId == UserId);
        if (!isEnrolled) return Forbid();

        var course = await db.Courses.FindAsync(courseId);
        if (course == null) return NotFound();

        var assignments = await db.Assignments
            .Include(a => a.Submissions.Where(s => s.UserId == UserId))
            .Where(a => a.CourseId == courseId)
            .ToListAsync();

        var quizzes = await db.Quizzes
            .Where(q => q.CourseId == courseId && q.IsPublished)
            .ToListAsync();

        var quizIds = quizzes.Select(q => q.Id).ToList();
        var attempts = await db.QuizAttempts
            .Where(a => quizIds.Contains(a.QuizId) && a.UserId == UserId && a.SubmittedAt != null)
            .ToListAsync();

        var assignmentItems = assignments.Select(a =>
        {
            var sub = a.Submissions.FirstOrDefault();
            return new GradeItem(a.Id, a.Title, sub?.Score, a.MaxScore,
                sub == null ? "not_submitted" : sub.Status.ToString().ToLower());
        }).ToList();

        var quizItems = quizzes.Select(q =>
        {
            var attempt = attempts.Where(a => a.QuizId == q.Id)
                .OrderByDescending(a => a.Score).FirstOrDefault();
            return new GradeItem(q.Id, q.Title,
                attempt?.Score, attempt?.MaxScore ?? 100,
                attempt == null ? "not_submitted" : "graded");
        }).ToList();

        var allItems = assignmentItems.Cast<GradeItem>().Concat(quizItems).ToList();
        var gradedItems = allItems.Where(i => i.Score.HasValue).ToList();
        double? pct = gradedItems.Any()
            ? gradedItems.Sum(i => (double)i.Score! / i.MaxScore * 100) / allItems.Count
            : null;

        return Ok(new GradebookMyView(
            courseId, course.Title,
            assignmentItems, quizItems,
            pct.HasValue ? Math.Round(pct.Value, 1) : null,
            ToLetterGrade(pct)));
    }

    // Teacher: export gradebook to CSV
    [HttpGet("export")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> Export(int courseId)
    {
        if (!await IsTeacherOrAdmin(courseId)) return Forbid();

        var enrollments = await db.Enrollments
            .Where(e => e.CourseId == courseId)
            .ToListAsync();

        var assignments = await db.Assignments
            .Include(a => a.Submissions)
            .Where(a => a.CourseId == courseId)
            .ToListAsync();

        var quizzes = await db.Quizzes
            .Where(q => q.CourseId == courseId && q.IsPublished)
            .ToListAsync();

        var attempts = await db.QuizAttempts
            .Where(a => a.Quiz.CourseId == courseId && a.SubmittedAt != null)
            .ToListAsync();

        var course = await db.Courses.FindAsync(courseId);

        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        // Write header
        csv.WriteField("Nama");
        csv.WriteField("User ID");
        foreach (var a in assignments) csv.WriteField($"Tugas: {a.Title} (/{a.MaxScore})");
        foreach (var q in quizzes) csv.WriteField($"Quiz: {q.Title} (/100)");
        csv.WriteField("Total %");
        csv.WriteField("Grade");
        await csv.NextRecordAsync();

        foreach (var e in enrollments)
        {
            csv.WriteField(e.UserName);
            csv.WriteField(e.UserId);

            double totalPct = 0; int gradedCount = 0;
            int itemCount = assignments.Count + quizzes.Count;

            foreach (var a in assignments)
            {
                var sub = a.Submissions.FirstOrDefault(s => s.UserId == e.UserId);
                csv.WriteField(sub?.Score?.ToString() ?? "-");
                if (sub?.Score != null) { totalPct += (double)sub.Score / a.MaxScore * 100; gradedCount++; }
            }
            foreach (var q in quizzes)
            {
                var attempt = attempts.Where(a => a.QuizId == q.Id && a.UserId == e.UserId)
                    .OrderByDescending(a => a.Score).FirstOrDefault();
                csv.WriteField(attempt?.Score != null ? $"{(double)attempt.Score / attempt.MaxScore * 100:F1}" : "-");
                if (attempt?.Score != null) { totalPct += (double)attempt.Score / attempt.MaxScore * 100; gradedCount++; }
            }

            double? pct = gradedCount > 0 ? totalPct / itemCount : null;
            csv.WriteField(pct.HasValue ? $"{pct.Value:F1}%" : "-");
            csv.WriteField(ToLetterGrade(pct));
            await csv.NextRecordAsync();
        }

        await writer.FlushAsync();
        var fileName = $"gradebook-{course?.Title?.Replace(" ", "_")}-{DateTime.UtcNow:yyyyMMdd}.csv";
        return File(ms.ToArray(), "text/csv", fileName);
    }

    private async Task<bool> IsTeacherOrAdmin(int courseId)
    {
        if (UserRole == "admin") return true;
        if (UserRole != "teacher") return false;
        return await db.Courses.AnyAsync(c => c.Id == courseId && c.InstructorId == UserId);
    }

    private static string ToLetterGrade(double? pct) => pct switch
    {
        >= 90 => "A",
        >= 80 => "B",
        >= 70 => "C",
        >= 60 => "D",
        < 60 when pct.HasValue => "E",
        _ => "-"
    };
}
