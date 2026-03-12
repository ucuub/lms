using LmsApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[Authorize]
public class CalendarController(LmsDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst("sub")?.Value ?? string.Empty;

        var courseIds = await db.Enrollments
            .Where(e => e.UserId == userId)
            .Select(e => e.CourseId)
            .ToListAsync();

        var events = await db.CalendarEvents
            .Include(e => e.Course)
            .Where(e => e.CourseId == null || courseIds.Contains(e.CourseId.Value))
            .Where(e => e.EventDate >= DateTime.UtcNow.AddDays(-30))
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return View(events);
    }
}
