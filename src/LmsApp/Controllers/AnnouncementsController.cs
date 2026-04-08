using LmsApp.Data;
using LmsApp.Models;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LmsApp.Controllers;

[ApiController]
[Route("api/courses/{courseId:int}/announcements")]
[Authorize]
public class AnnouncementsController(LmsDbContext db, INotificationService notifService) : ControllerBase
{
    private string UserId => User.FindFirst("sub")?.Value ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";
    private string UserName => User.FindFirst("name")?.Value ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> GetAll(int courseId)
    {
        if (!await CanAccess(courseId)) return Forbid();

        var announcements = await db.Announcements
            .Where(a => a.CourseId == courseId)
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

        return Ok(announcements);
    }

    [HttpPost]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> Create(int courseId, AnnouncementRequest req)
    {
        if (!await IsTeacherOrAdmin(courseId)) return Forbid();

        var course = await db.Courses.FindAsync(courseId);
        if (course == null) return NotFound();

        var announcement = new Announcement
        {
            CourseId = courseId,
            Title = req.Title,
            Content = req.Content,
            IsPinned = req.IsPinned,
            AuthorName = UserName
        };
        db.Announcements.Add(announcement);
        await db.SaveChangesAsync();

        // Broadcast notification to all enrolled students
        var studentIds = await db.Enrollments
            .Where(e => e.CourseId == courseId && e.UserId != UserId)
            .Select(e => e.UserId)
            .ToListAsync();

        foreach (var studentId in studentIds)
        {
            await notifService.CreateAsync(studentId, $"📢 {course.Title}",
                $"Pengumuman baru: {req.Title}",
                NotificationType.Announcement,
                $"/courses/{courseId}/announcements");
        }

        return Ok(announcement);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> Delete(int courseId, int id)
    {
        if (!await IsTeacherOrAdmin(courseId)) return Forbid();

        var announcement = await db.Announcements.FindAsync(id);
        if (announcement == null || announcement.CourseId != courseId) return NotFound();

        db.Announcements.Remove(announcement);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> CanAccess(int courseId)
    {
        if (UserRole is "teacher" or "admin") return true;
        return await db.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserId == UserId);
    }

    private async Task<bool> IsTeacherOrAdmin(int courseId)
    {
        if (UserRole == "admin") return true;
        if (UserRole != "teacher") return false;
        return await db.Courses.AnyAsync(c => c.Id == courseId && c.InstructorId == UserId);
    }
}

public record AnnouncementRequest(
    [Required, MaxLength(200)] string Title,
    [Required] string Content,
    bool IsPinned = false
);
