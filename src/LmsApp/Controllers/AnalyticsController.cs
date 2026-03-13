using LmsApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[Authorize(Roles = "instructor,admin")]
public class AnalyticsController(LmsDbContext db) : Controller
{
    private string UserId => User.FindFirst("sub")?.Value
        ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

    // GET /Analytics/Course/{id}  — instruktur lihat laporan 1 kursus
    public async Task<IActionResult> Course(int id)
    {
        var course = await db.Courses
            .Include(c => c.Enrollments)
            .Include(c => c.Modules)
            .Include(c => c.Assignments).ThenInclude(a => a.Submissions)
            .Include(c => c.Quizzes).ThenInclude(q => q.Attempts)
            .Include(c => c.Reviews)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course is null) return NotFound();
        if (course.InstructorId != UserId && !User.IsInRole("admin")) return Forbid();

        // Enrollment per bulan (6 bulan terakhir)
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var enrollmentByMonth = await db.Enrollments
            .Where(e => e.CourseId == id && e.EnrolledAt >= sixMonthsAgo)
            .GroupBy(e => new { e.EnrolledAt.Year, e.EnrolledAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync();

        // Progress peserta
        var progressList = await db.CourseProgresses
            .Where(p => p.CourseId == id)
            .OrderByDescending(p => p.Percentage)
            .Take(20)
            .ToListAsync();

        // Quiz stats
        var quizStats = course.Quizzes.Select(q => new
        {
            q.Title,
            AttemptCount = q.Attempts.Count,
            AvgScore = q.Attempts.Any()
                ? Math.Round(q.Attempts.Average(a => (double)a.Score / Math.Max(a.MaxScore, 1) * 100), 1)
                : 0.0,
            PassRate = q.Attempts.Any()
                ? Math.Round(q.Attempts.Count(a => a.IsPassed) * 100.0 / q.Attempts.Count, 1)
                : 0.0
        }).ToList();

        // Assignment stats
        var assignmentStats = course.Assignments.Select(a => new
        {
            a.Title,
            SubmissionCount = a.Submissions.Count,
            GradedCount = a.Submissions.Count(s => s.Score.HasValue),
            AvgScore = a.Submissions.Any(s => s.Score.HasValue)
                ? Math.Round(a.Submissions.Where(s => s.Score.HasValue).Average(s => (double)s.Score!.Value), 1)
                : 0.0
        }).ToList();

        // Top performers (by module completion %)
        var topPerformers = progressList.Take(5).ToList();

        ViewBag.Course = course;
        ViewBag.EnrollmentByMonth = enrollmentByMonth;
        ViewBag.ProgressList = progressList;
        ViewBag.QuizStats = quizStats;
        ViewBag.AssignmentStats = assignmentStats;
        ViewBag.TopPerformers = topPerformers;
        ViewBag.AvgRating = course.Reviews.Any()
            ? Math.Round(course.Reviews.Average(r => r.Rating), 1) : 0.0;
        ViewBag.CompletionRate = course.Enrollments.Any()
            ? Math.Round(course.Enrollments.Count(e => e.Status == LmsApp.Models.EnrollmentStatus.Completed)
              * 100.0 / course.Enrollments.Count, 1) : 0.0;

        return View(course);
    }

    // GET /Analytics/Overview  — admin overview seluruh platform
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Overview()
    {
        // Total stats
        ViewBag.TotalUsers = await db.AppUsers.CountAsync();
        ViewBag.TotalCourses = await db.Courses.CountAsync(c => c.IsPublished);
        ViewBag.TotalEnrollments = await db.Enrollments.CountAsync();
        ViewBag.TotalCompletions = await db.Enrollments.CountAsync(e => e.Status == LmsApp.Models.EnrollmentStatus.Completed);
        ViewBag.TotalQuizAttempts = await db.QuizAttempts.CountAsync();
        ViewBag.TotalSubmissions = await db.Submissions.CountAsync();
        ViewBag.TotalCertificates = await db.Certificates.CountAsync();

        // Top courses by enrollment
        var topCourses = await db.Courses
            .Where(c => c.IsPublished)
            .Select(c => new
            {
                c.Id, c.Title, c.InstructorName,
                EnrollCount = db.Enrollments.Count(e => e.CourseId == c.Id),
                AvgRating = db.CourseReviews.Where(r => r.CourseId == c.Id).Any()
                    ? db.CourseReviews.Where(r => r.CourseId == c.Id).Average(r => r.Rating) : 0.0
            })
            .OrderByDescending(c => c.EnrollCount)
            .Take(10)
            .ToListAsync();

        // Enrollment trend (12 bulan)
        var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);
        var enrollmentTrend = await db.Enrollments
            .Where(e => e.EnrolledAt >= twelveMonthsAgo)
            .GroupBy(e => new { e.EnrolledAt.Year, e.EnrolledAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync();

        // Top instructors
        var topInstructors = await db.Courses
            .Where(c => c.IsPublished)
            .GroupBy(c => new { c.InstructorId, c.InstructorName })
            .Select(g => new
            {
                g.Key.InstructorName,
                CourseCount = g.Count(),
                TotalEnroll = g.Sum(c => db.Enrollments.Count(e => e.CourseId == c.Id))
            })
            .OrderByDescending(i => i.TotalEnroll)
            .Take(5)
            .ToListAsync();

        ViewBag.TopCourses = topCourses;
        ViewBag.EnrollmentTrend = enrollmentTrend;
        ViewBag.TopInstructors = topInstructors;

        return View();
    }
}
