using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class AssignmentsController(LmsDbContext db, IFileUploadService fileService) : ControllerBase
{
    private string UserId => User.FindFirst("sub")?.Value ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";
    private string UserName => User.FindFirst("name")?.Value ?? string.Empty;

    // ── Assignment CRUD ───────────────────────────────────────────────────────

    [HttpGet("courses/{courseId:int}/assignments")]
    public async Task<ActionResult<IEnumerable<AssignmentResponse>>> GetByCourse(int courseId)
    {
        var isTeacher = await IsTeacherOrAdmin(courseId);
        var isEnrolled = await db.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserId == UserId);
        if (!isTeacher && !isEnrolled) return Forbid();

        var assignments = await db.Assignments
            .Include(a => a.Submissions)
            .Where(a => a.CourseId == courseId)
            .OrderBy(a => a.DueDate ?? DateTime.MaxValue)
            .ToListAsync();

        return Ok(assignments.Select(a => ToResponse(a)));
    }

    [HttpGet("assignments/{id:int}")]
    public async Task<ActionResult<AssignmentResponse>> GetById(int id)
    {
        var assignment = await db.Assignments
            .Include(a => a.Submissions)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (assignment == null) return NotFound();

        var isTeacher = await IsTeacherOrAdmin(assignment.CourseId);
        var isEnrolled = await db.Enrollments.AnyAsync(e => e.CourseId == assignment.CourseId && e.UserId == UserId);
        if (!isTeacher && !isEnrolled) return Forbid();

        return Ok(ToResponse(assignment));
    }

    [HttpPost("courses/{courseId:int}/assignments")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<AssignmentResponse>> Create(int courseId, AssignmentRequest req)
    {
        if (!await IsTeacherOrAdmin(courseId)) return Forbid();

        var assignment = new Assignment
        {
            CourseId = courseId,
            Title = req.Title,
            Description = req.Description,
            DueDate = req.DueDate,
            MaxScore = req.MaxScore
        };
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = assignment.Id }, ToResponse(assignment));
    }

    [HttpPut("assignments/{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<AssignmentResponse>> Update(int id, AssignmentRequest req)
    {
        var assignment = await db.Assignments.Include(a => a.Submissions).FirstOrDefaultAsync(a => a.Id == id);
        if (assignment == null) return NotFound();
        if (!await IsTeacherOrAdmin(assignment.CourseId)) return Forbid();

        assignment.Title = req.Title;
        assignment.Description = req.Description;
        assignment.DueDate = req.DueDate;
        assignment.MaxScore = req.MaxScore;
        await db.SaveChangesAsync();
        return Ok(ToResponse(assignment));
    }

    [HttpDelete("assignments/{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var assignment = await db.Assignments.FindAsync(id);
        if (assignment == null) return NotFound();
        if (!await IsTeacherOrAdmin(assignment.CourseId)) return Forbid();

        db.Assignments.Remove(assignment);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Submissions ───────────────────────────────────────────────────────────

    // Student: submit assignment
    [HttpPost("assignments/{id:int}/submit")]
    public async Task<ActionResult<SubmissionResponse>> Submit(int id,
        [FromForm] string? textContent,
        IFormFile? file)
    {
        var assignment = await db.Assignments.FindAsync(id);
        if (assignment == null) return NotFound();

        var isEnrolled = await db.Enrollments.AnyAsync(e => e.CourseId == assignment.CourseId && e.UserId == UserId);
        if (!isEnrolled) return Forbid();

        if (string.IsNullOrWhiteSpace(textContent) && file == null)
            return BadRequest(new { message = "Konten atau file wajib diisi." });

        string? fileUrl = null, fileName = null;
        if (file != null)
        {
            if (!fileService.IsValidFile(file, [".pdf", ".doc", ".docx", ".zip", ".txt", ".png", ".jpg"], 20 * 1024 * 1024))
                return BadRequest(new { message = "File tidak valid. Maks 20MB." });
            fileUrl = await fileService.UploadAsync(file, "submissions");
            fileName = file.FileName;
        }

        var existing = await db.Submissions.FirstOrDefaultAsync(s => s.AssignmentId == id && s.UserId == UserId);
        if (existing != null)
        {
            if (existing.FileUrl != null) fileService.Delete(existing.FileUrl);
            existing.TextContent = textContent;
            existing.FileUrl = fileUrl;
            existing.FileName = fileName;
            existing.SubmittedAt = DateTime.UtcNow;
            existing.Status = SubmissionStatus.Submitted;
        }
        else
        {
            existing = new Submission
            {
                AssignmentId = id,
                UserId = UserId,
                UserName = UserName,
                TextContent = textContent,
                FileUrl = fileUrl,
                FileName = fileName,
                Status = SubmissionStatus.Submitted
            };
            db.Submissions.Add(existing);
        }

        await db.SaveChangesAsync();
        return Ok(ToSubmissionResponse(existing, assignment.Title));
    }

    // Teacher: get all submissions for assignment
    [HttpGet("assignments/{id:int}/submissions")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<IEnumerable<SubmissionResponse>>> GetSubmissions(int id)
    {
        var assignment = await db.Assignments.FindAsync(id);
        if (assignment == null) return NotFound();
        if (!await IsTeacherOrAdmin(assignment.CourseId)) return Forbid();

        var submissions = await db.Submissions
            .Where(s => s.AssignmentId == id)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();

        return Ok(submissions.Select(s => ToSubmissionResponse(s, assignment.Title)));
    }

    // Teacher: grade submission
    [HttpPost("submissions/{submissionId:int}/grade")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<SubmissionResponse>> Grade(int submissionId, GradeSubmissionRequest req)
    {
        var submission = await db.Submissions
            .Include(s => s.Assignment)
            .FirstOrDefaultAsync(s => s.Id == submissionId);
        if (submission == null) return NotFound();
        if (!await IsTeacherOrAdmin(submission.Assignment.CourseId)) return Forbid();

        if (req.Score > submission.Assignment.MaxScore)
            return BadRequest(new { message = $"Skor melebihi nilai maksimum ({submission.Assignment.MaxScore})." });

        submission.Score = req.Score;
        submission.Feedback = req.Feedback;
        submission.Status = SubmissionStatus.Graded;
        submission.GradedAt = DateTime.UtcNow;

        // Notification to student
        db.Notifications.Add(new Notification
        {
            UserId = submission.UserId,
            Title = "Tugas Dinilai",
            Message = $"Tugas \"{submission.Assignment.Title}\" telah dinilai: {req.Score}/{submission.Assignment.MaxScore}.",
            Type = NotificationType.Grade,
            Link = $"/courses/{submission.Assignment.CourseId}/assignments/{submission.AssignmentId}"
        });

        await db.SaveChangesAsync();
        return Ok(ToSubmissionResponse(submission, submission.Assignment.Title));
    }

    // Student: get my submission
    [HttpGet("assignments/{id:int}/my-submission")]
    public async Task<ActionResult<SubmissionResponse>> MySubmission(int id)
    {
        var assignment = await db.Assignments.FindAsync(id);
        if (assignment == null) return NotFound();

        var submission = await db.Submissions.FirstOrDefaultAsync(s => s.AssignmentId == id && s.UserId == UserId);
        if (submission == null) return NotFound();

        return Ok(ToSubmissionResponse(submission, assignment.Title));
    }

    private async Task<bool> IsTeacherOrAdmin(int courseId)
    {
        if (UserRole == "admin") return true;
        if (UserRole != "teacher") return false;
        return await db.Courses.AnyAsync(c => c.Id == courseId && c.InstructorId == UserId);
    }

    private static AssignmentResponse ToResponse(Assignment a) => new(
        a.Id, a.CourseId, a.Title, a.Description, a.DueDate, a.MaxScore,
        a.Submissions?.Count ?? 0,
        a.Submissions?.Count(s => s.Status == SubmissionStatus.Graded) ?? 0,
        a.CreatedAt);

    private static SubmissionResponse ToSubmissionResponse(Submission s, string assignmentTitle) => new(
        s.Id, s.AssignmentId, assignmentTitle, s.UserId, s.UserName,
        s.TextContent, s.FileUrl, s.FileName,
        s.Score, s.Feedback, s.Status.ToString(),
        s.SubmittedAt, s.GradedAt);
}
