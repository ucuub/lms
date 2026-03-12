using LmsApp.Data;
using LmsApp.Models;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[Authorize]
public class SubmissionController(LmsDbContext db, IFileUploadService fileUpload, INotificationService notifications) : Controller
{
    // GET: /Submission/Submit/assignmentId
    public async Task<IActionResult> Submit(int assignmentId)
    {
        var assignment = await db.Assignments
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment is null) return NotFound();

        var userId = User.FindFirst("sub")?.Value ?? string.Empty;
        var existing = await db.Submissions
            .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.UserId == userId);

        ViewBag.Assignment = assignment;
        ViewBag.Existing = existing;
        return View();
    }

    // POST: /Submission/Submit
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int assignmentId, string? content, IFormFile? file)
    {
        var assignment = await db.Assignments.Include(a => a.Course).FirstOrDefaultAsync(a => a.Id == assignmentId);
        if (assignment is null) return NotFound();

        var userId = User.FindFirst("sub")?.Value ?? string.Empty;

        // Cek batas tenggat
        if (assignment.DueDate.HasValue && DateTime.UtcNow > assignment.DueDate.Value)
        {
            TempData["Error"] = "Tenggat waktu sudah lewat.";
            return RedirectToAction(nameof(Submit), new { assignmentId });
        }

        string? fileUrl = null;
        if (file is not null && file.Length > 0)
        {
            if (!fileUpload.IsValidFile(file, [".pdf", ".doc", ".docx", ".zip", ".png", ".jpg"]))
            {
                TempData["Error"] = "Format file tidak didukung atau ukuran melebihi 10MB.";
                return RedirectToAction(nameof(Submit), new { assignmentId });
            }
            fileUrl = await fileUpload.UploadAsync(file, "submissions");
        }

        var existing = await db.Submissions
            .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.UserId == userId);

        if (existing is not null)
        {
            // Update existing submission
            if (existing.FileUrl is not null && fileUrl is not null)
                fileUpload.Delete(existing.FileUrl);

            existing.Content = content;
            existing.FileUrl = fileUrl ?? existing.FileUrl;
            existing.SubmittedAt = DateTime.UtcNow;
            existing.Score = null;
            existing.Feedback = null;
            existing.GradedAt = null;
        }
        else
        {
            db.Submissions.Add(new Submission
            {
                AssignmentId = assignmentId,
                UserId = userId,
                UserName = User.Identity?.Name ?? "Unknown",
                Content = content,
                FileUrl = fileUrl
            });
        }

        await db.SaveChangesAsync();
        TempData["Success"] = "Tugas berhasil dikumpulkan!";
        return RedirectToAction("Details", "Course", new { id = assignment.CourseId });
    }

    // GET: /Submission/Grade/assignmentId  — instruktur melihat semua submission
    [Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> Grade(int assignmentId)
    {
        var assignment = await db.Assignments
            .Include(a => a.Course)
            .Include(a => a.Submissions)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment is null) return NotFound();
        return View(assignment);
    }

    // POST: /Submission/GradeOne  — instruktur nilai satu submission
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> GradeOne(int submissionId, int score, string? feedback)
    {
        var submission = await db.Submissions
            .Include(s => s.Assignment)
            .FirstOrDefaultAsync(s => s.Id == submissionId);

        if (submission is null) return NotFound();

        submission.Score = Math.Clamp(score, 0, submission.Assignment.MaxScore);
        submission.Feedback = feedback;
        submission.GradedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        // Kirim notifikasi ke peserta
        await notifications.CreateForGradeAsync(
            submission.UserId,
            submission.Assignment.Title,
            submission.Score.Value,
            $"/Submission/MyResult/{submission.Id}");

        TempData["Success"] = $"Nilai untuk {submission.UserName} berhasil disimpan.";
        return RedirectToAction(nameof(Grade), new { assignmentId = submission.AssignmentId });
    }

    // GET: /Submission/MyResult/5  — peserta lihat hasil
    public async Task<IActionResult> MyResult(int id)
    {
        var submission = await db.Submissions
            .Include(s => s.Assignment).ThenInclude(a => a.Course)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (submission is null) return NotFound();

        var userId = User.FindFirst("sub")?.Value;
        if (submission.UserId != userId) return Forbid();

        return View(submission);
    }
}
