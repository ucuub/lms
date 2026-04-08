using LmsApp.Data;
using LmsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[ApiController]
[Route("api/calendar")]
[Authorize]
public class CalendarController(LmsDbContext db) : ControllerBase
{
    private string UserId   => User.FindFirst("sub")?.Value  ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";

    // GET /api/calendar?from=2025-01-01&to=2025-01-31
    // Returns events relevant to the current user:
    //   - global events (no courseId)
    //   - events from enrolled/instructing courses
    //   - assignment deadlines from enrolled courses
    //   - quiz deadlines from enrolled courses
    [HttpGet]
    public async Task<IActionResult> GetEvents(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var start = from ?? DateTime.UtcNow.AddMonths(-1);
        var end   = to   ?? DateTime.UtcNow.AddMonths(3);

        // Courses the user can see
        List<int> courseIds;
        if (UserRole is "teacher" or "admin")
        {
            courseIds = await db.Courses
                .Where(c => UserRole == "admin" || c.InstructorId == UserId)
                .Select(c => c.Id)
                .ToListAsync();
        }
        else
        {
            courseIds = await db.Enrollments
                .Where(e => e.UserId == UserId)
                .Select(e => e.CourseId)
                .ToListAsync();
        }

        // Manual calendar events
        var calEvents = await db.CalendarEvents
            .Where(e =>
                e.EventDate >= start && e.EventDate <= end &&
                (e.CourseId == null || courseIds.Contains(e.CourseId.Value)))
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        var result = calEvents.Select(e => new CalendarEventDto(
            e.Id, e.CourseId, e.Title, e.Description,
            e.EventDate, e.Type.ToString(), e.CreatedByUserId)).ToList();

        // Assignment deadlines
        var assignments = await db.Assignments
            .Where(a => courseIds.Contains(a.CourseId) &&
                        a.DueDate.HasValue &&
                        a.DueDate >= start && a.DueDate <= end)
            .ToListAsync();

        foreach (var a in assignments)
        {
            result.Add(new CalendarEventDto(
                -a.Id, a.CourseId, $"[Tugas] {a.Title}", a.Description,
                a.DueDate!.Value, CalendarEventType.Assignment.ToString(), null));
        }

        // Quiz deadlines
        var quizzes = await db.Quizzes
            .Where(q => courseIds.Contains(q.CourseId) &&
                        q.IsPublished &&
                        q.DueDate.HasValue &&
                        q.DueDate >= start && q.DueDate <= end)
            .ToListAsync();

        foreach (var q in quizzes)
        {
            result.Add(new CalendarEventDto(
                -q.Id * 1000, q.CourseId, $"[Quiz] {q.Title}", null,
                q.DueDate!.Value, CalendarEventType.Quiz.ToString(), null));
        }

        return Ok(result.OrderBy(e => e.EventDate));
    }

    // POST /api/calendar
    [HttpPost]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> Create(CreateCalendarEventRequest req)
    {
        if (req.CourseId.HasValue && !await CanManageCourse(req.CourseId.Value))
            return Forbid();

        var ev = new CalendarEvent
        {
            CourseId = req.CourseId,
            Title = req.Title,
            Description = req.Description,
            EventDate = req.EventDate.ToUniversalTime(),
            Type = Enum.TryParse<CalendarEventType>(req.Type, true, out var t) ? t : CalendarEventType.Event,
            CreatedByUserId = UserId
        };
        db.CalendarEvents.Add(ev);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetEvents), null, ToDto(ev));
    }

    // PUT /api/calendar/{id}
    [HttpPut("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> Update(int id, CreateCalendarEventRequest req)
    {
        var ev = await db.CalendarEvents.FindAsync(id);
        if (ev == null) return NotFound();

        if (ev.CreatedByUserId != UserId && UserRole != "admin") return Forbid();

        ev.Title = req.Title;
        ev.Description = req.Description;
        ev.EventDate = req.EventDate.ToUniversalTime();
        ev.Type = Enum.TryParse<CalendarEventType>(req.Type, true, out var t) ? t : ev.Type;
        await db.SaveChangesAsync();
        return Ok(ToDto(ev));
    }

    // DELETE /api/calendar/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await db.CalendarEvents.FindAsync(id);
        if (ev == null) return NotFound();

        if (ev.CreatedByUserId != UserId && UserRole != "admin") return Forbid();

        db.CalendarEvents.Remove(ev);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> CanManageCourse(int courseId)
    {
        if (UserRole == "admin") return true;
        return await db.Courses.AnyAsync(c => c.Id == courseId && c.InstructorId == UserId);
    }

    private static CalendarEventDto ToDto(CalendarEvent e) => new(
        e.Id, e.CourseId, e.Title, e.Description,
        e.EventDate, e.Type.ToString(), e.CreatedByUserId);
}

public record CalendarEventDto(
    int Id,
    int? CourseId,
    string Title,
    string? Description,
    DateTime EventDate,
    string Type,
    string? CreatedByUserId
);

public record CreateCalendarEventRequest(
    string Title,
    string? Description,
    DateTime EventDate,
    string Type,   // "Event" | "Assignment" | "Quiz" | "Announcement"
    int? CourseId
);
