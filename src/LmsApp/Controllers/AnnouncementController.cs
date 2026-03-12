using LmsApp.Data;
using LmsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[Authorize]
public class AnnouncementController(LmsDbContext db) : Controller
{
    // GET: /Announcement/Course/5
    public async Task<IActionResult> Course(int courseId)
    {
        var course = await db.Courses.FindAsync(courseId);
        if (course is null) return NotFound();

        var announcements = await db.Announcements
            .Where(a => a.CourseId == courseId)
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

        ViewBag.Course = course;
        ViewBag.CourseId = courseId;
        return View(announcements);
    }

    // GET: /Announcement/Create?courseId=5
    [Authorize(Roles = "instructor,admin")]
    public IActionResult Create(int courseId)
    {
        ViewBag.CourseId = courseId;
        return View();
    }

    // POST: /Announcement/Create
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> Create(int courseId, string title, string content, bool isPinned)
    {
        db.Announcements.Add(new Announcement
        {
            CourseId = courseId,
            Title = title,
            Content = content,
            IsPinned = isPinned,
            AuthorId = User.FindFirst("sub")?.Value ?? string.Empty,
            AuthorName = User.Identity?.Name ?? "Unknown"
        });

        // Add calendar event for announcement
        db.CalendarEvents.Add(new CalendarEvent
        {
            CourseId = courseId,
            Title = $"Pengumuman: {title}",
            EventDate = DateTime.UtcNow,
            Type = CalendarEventType.Announcement
        });

        await db.SaveChangesAsync();
        TempData["Success"] = "Pengumuman berhasil dikirim!";
        return RedirectToAction(nameof(Course), new { courseId });
    }

    // POST: /Announcement/Delete/5
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var announcement = await db.Announcements.FindAsync(id);
        if (announcement is null) return NotFound();

        var courseId = announcement.CourseId;
        db.Announcements.Remove(announcement);
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Course), new { courseId });
    }
}
