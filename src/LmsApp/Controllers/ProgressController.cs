using LmsApp.Data;
using LmsApp.Models;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[Authorize]
public class ProgressController(LmsDbContext db, INotificationService notifications) : Controller
{
    // POST: /Progress/MarkModuleComplete
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkModuleComplete(int moduleId, int courseId)
    {
        var userId = User.FindFirst("sub")?.Value ?? string.Empty;

        // Cek apakah sudah pernah ditandai
        var already = await db.ModuleProgresses
            .AnyAsync(mp => mp.ModuleId == moduleId && mp.UserId == userId);

        if (!already)
        {
            // Ambil atau buat CourseProgress
            var cp = await db.CourseProgresses
                .FirstOrDefaultAsync(p => p.CourseId == courseId && p.UserId == userId);

            var totalModules = await db.CourseModules.CountAsync(m => m.CourseId == courseId);

            if (cp is null)
            {
                cp = new CourseProgress
                {
                    CourseId = courseId,
                    UserId = userId,
                    TotalModules = totalModules
                };
                db.CourseProgresses.Add(cp);
                await db.SaveChangesAsync();
            }

            db.ModuleProgresses.Add(new ModuleProgress
            {
                CourseProgressId = cp.Id,
                ModuleId = moduleId,
                UserId = userId
            });

            cp.CompletedModules = await db.ModuleProgresses
                .CountAsync(mp => mp.UserId == userId &&
                    db.CourseModules.Where(m => m.CourseId == courseId).Select(m => m.Id).Contains(mp.ModuleId)) + 1;

            cp.TotalModules = totalModules;
            cp.LastAccessedAt = DateTime.UtcNow;

            // Jika semua modul selesai
            if (cp.CompletedModules >= cp.TotalModules)
            {
                cp.CompletedAt = DateTime.UtcNow;
                var course = await db.Courses.FindAsync(courseId);
                await notifications.CreateAsync(userId,
                    "Kursus Selesai! 🎉",
                    $"Selamat! Kamu telah menyelesaikan kursus \"{course?.Title}\". Ambil sertifikatmu sekarang.",
                    NotificationType.Success,
                    $"/Certificate/Generate/{courseId}");
            }

            await db.SaveChangesAsync();
        }

        return RedirectToAction("Details", "Course", new { id = courseId });
    }

    // GET: /Progress/Course/5  — API-style untuk AJAX
    public async Task<IActionResult> Course(int courseId)
    {
        var userId = User.FindFirst("sub")?.Value ?? string.Empty;

        var progress = await db.CourseProgresses
            .Include(p => p.ModuleProgresses)
            .FirstOrDefaultAsync(p => p.CourseId == courseId && p.UserId == userId);

        if (progress is null) return Json(new { percentage = 0, completedModules = 0, totalModules = 0 });

        return Json(new
        {
            percentage = progress.Percentage,
            completedModules = progress.CompletedModules,
            totalModules = progress.TotalModules,
            completedModuleIds = progress.ModuleProgresses.Select(mp => mp.ModuleId)
        });
    }
}
