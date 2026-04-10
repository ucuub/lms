using LmsApp.Data;
using LmsApp.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[ApiController]
[Route("api/activity")]
[Authorize]
public class ActivityController(LmsDbContext db) : ControllerBase
{
    private string UserId   => User.FindFirst("sub")?.Value  ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";
    private bool IsTeacherOrAdmin => UserRole is "teacher" or "admin";

    // GET /api/activity/me?page=1&limit=20
    [HttpGet("me")]
    public async Task<ActionResult<IEnumerable<ActivityLogDto>>> GetMyActivity(
        [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var logs = await db.ActivityLogs
            .Where(a => a.UserId == UserId)
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return Ok(logs.Select(ToDto));
    }

    // GET /api/activity/course/{courseId}  (teacher/admin only)
    [HttpGet("course/{courseId:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<IEnumerable<ActivityLogDto>>> GetCourseActivity(
        int courseId, [FromQuery] int page = 1, [FromQuery] int limit = 50)
    {
        var course = await db.Courses.FindAsync(courseId);
        if (course == null) return NotFound();
        if (UserRole != "admin" && course.InstructorId != UserId) return Forbid();

        var logs = await db.ActivityLogs
            .Where(a => a.CourseId == courseId)
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return Ok(logs.Select(ToDto));
    }

    private static ActivityLogDto ToDto(Models.ActivityLog a) =>
        new(a.Id, a.UserId, a.UserName, a.Action, a.EntityType,
            a.EntityId, a.EntityTitle, a.CourseId, a.Metadata, a.Timestamp);
}
