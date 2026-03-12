using LmsApp.Data;
using LmsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[Authorize]
public class CertificateController(LmsDbContext db) : Controller
{
    // GET: /Certificate/View/5
    public async Task<IActionResult> View(int id)
    {
        var cert = await db.Certificates
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cert is null) return NotFound();

        var userId = User.FindFirst("sub")?.Value;
        if (cert.UserId != userId && !User.IsInRole("admin")) return Forbid();

        return View(cert);
    }

    // POST: /Certificate/Generate/courseId
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(int courseId)
    {
        var userId = User.FindFirst("sub")?.Value ?? string.Empty;

        // Check if course is completed
        var progress = await db.CourseProgresses
            .FirstOrDefaultAsync(p => p.CourseId == courseId && p.UserId == userId);

        if (progress is null || progress.Percentage < 100)
        {
            TempData["Error"] = "Selesaikan semua modul terlebih dahulu.";
            return RedirectToAction("Details", "Course", new { id = courseId });
        }

        // Check if already has certificate
        var existing = await db.Certificates
            .FirstOrDefaultAsync(c => c.CourseId == courseId && c.UserId == userId);

        if (existing is not null)
            return RedirectToAction(nameof(View), new { id = existing.Id });

        var cert = new Certificate
        {
            CourseId = courseId,
            UserId = userId,
            UserName = User.Identity?.Name ?? "Unknown",
            CertificateNumber = $"LMS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}"
        };

        db.Certificates.Add(cert);
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(View), new { id = cert.Id });
    }
}
