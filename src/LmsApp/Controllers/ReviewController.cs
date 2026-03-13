using LmsApp.Data;
using LmsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[Authorize]
public class ReviewController(LmsDbContext db) : Controller
{
    private string UserId => User.FindFirst("sub")?.Value
        ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
    private string UserName => User.FindFirst("name")?.Value ?? User.Identity?.Name ?? "Pengguna";

    // POST /Review/Submit
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int courseId, int rating, string? comment)
    {
        rating = Math.Clamp(rating, 1, 5);

        // Must be enrolled
        bool enrolled = await db.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserId == UserId);
        if (!enrolled)
        {
            TempData["Error"] = "Kamu harus terdaftar di kursus ini untuk memberi ulasan.";
            return RedirectToAction("Details", "Course", new { id = courseId });
        }

        var existing = await db.CourseReviews
            .FirstOrDefaultAsync(r => r.CourseId == courseId && r.UserId == UserId);

        if (existing is not null)
        {
            existing.Rating = rating;
            existing.Comment = comment?.Trim();
            existing.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            db.CourseReviews.Add(new CourseReview
            {
                CourseId = courseId,
                UserId = UserId,
                UserName = UserName,
                Rating = rating,
                Comment = comment?.Trim()
            });
        }

        await db.SaveChangesAsync();
        TempData["Success"] = "Ulasanmu berhasil disimpan. Terima kasih!";
        return RedirectToAction("Details", "Course", new { id = courseId });
    }

    // POST /Review/Delete
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await db.CourseReviews.FindAsync(id);
        if (review is null) return NotFound();

        if (review.UserId != UserId && !User.IsInRole("admin")) return Forbid();

        var courseId = review.CourseId;
        db.CourseReviews.Remove(review);
        await db.SaveChangesAsync();

        TempData["Success"] = "Ulasan dihapus.";
        return RedirectToAction("Details", "Course", new { id = courseId });
    }
}
