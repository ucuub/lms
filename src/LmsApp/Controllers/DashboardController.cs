using LmsApp.Data;
using LmsApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[Authorize]
public class DashboardController(LmsDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst("sub")?.Value ?? string.Empty;

        var enrollments = await db.Enrollments
            .Include(e => e.Course)
            .Where(e => e.UserId == userId)
            .ToListAsync();

        var courseIds = enrollments.Select(e => e.CourseId).ToList();

        var progresses = await db.CourseProgresses
            .Where(p => p.UserId == userId && courseIds.Contains(p.CourseId))
            .ToListAsync();

        var upcomingDeadlines = await db.CalendarEvents
            .Where(ev => courseIds.Contains(ev.CourseId ?? 0) && ev.EventDate >= DateTime.UtcNow)
            .OrderBy(ev => ev.EventDate)
            .Take(5)
            .ToListAsync();

        var recentAnnouncements = await db.Announcements
            .Where(a => courseIds.Contains(a.CourseId))
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .ToListAsync();

        var certificates = await db.Certificates
            .Include(c => c.Course)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        var vm = new DashboardViewModel
        {
            Enrollments = enrollments,
            Progresses = progresses,
            UpcomingDeadlines = upcomingDeadlines,
            RecentAnnouncements = recentAnnouncements,
            Certificates = certificates
        };

        return View(vm);
    }
}
