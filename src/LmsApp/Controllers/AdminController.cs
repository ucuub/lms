using LmsApp.Data;
using LmsApp.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminController(LmsDbContext db) : ControllerBase
{
    // GET /api/admin/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalUsers = await db.AppUsers.CountAsync();
        var totalCourses = await db.Courses.CountAsync();
        var totalEnrollments = await db.Enrollments.CountAsync();
        var totalCertificates = await db.Certificates.CountAsync();
        var totalSubmissions = await db.Submissions.CountAsync();
        var totalAttempts = await db.QuizAttempts.CountAsync(a => a.SubmittedAt != null);

        // Enrollment trend (last 6 months)
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var trend = await db.Enrollments
            .Where(e => e.EnrolledAt >= sixMonthsAgo)
            .GroupBy(e => new { e.EnrolledAt.Year, e.EnrolledAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        // Top courses
        var topCourses = await db.Courses
            .Include(c => c.Enrollments)
            .OrderByDescending(c => c.Enrollments.Count)
            .Take(10)
            .Select(c => new { c.Id, c.Title, c.InstructorName, EnrollmentCount = c.Enrollments.Count })
            .ToListAsync();

        return Ok(new
        {
            totalUsers, totalCourses, totalEnrollments,
            totalCertificates, totalSubmissions, totalAttempts,
            enrollmentTrend = trend,
            topCourses
        });
    }

    // GET /api/admin/users
    [HttpGet("users")]
    public async Task<ActionResult<PagedResponse<UserDto>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = db.AppUsers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(u => u.Name.ToLower().Contains(s) || u.Email.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role == role);

        var total = await query.CountAsync();
        var users = await query.OrderBy(u => u.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new PagedResponse<UserDto>(
            users.Select(u => new UserDto(u.Id, u.UserId, u.Name, u.Email, u.Role, u.AvatarUrl, u.IsActive, u.CreatedAt)),
            total, page, pageSize, (int)Math.Ceiling(total / (double)pageSize)));
    }

    // PUT /api/admin/users/{userId}/role
    [HttpPut("users/{userId}/role")]
    public async Task<IActionResult> SetRole(string userId, [FromBody] SetRoleRequest req)
    {
        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return NotFound();

        user.Role = req.Role;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // PUT /api/admin/users/{userId}/toggle-active
    [HttpPut("users/{userId}/toggle-active")]
    public async Task<IActionResult> ToggleActive(string userId)
    {
        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return NotFound();

        user.IsActive = !user.IsActive;
        await db.SaveChangesAsync();
        return Ok(new { isActive = user.IsActive });
    }

    // GET /api/admin/courses
    [HttpGet("courses")]
    public async Task<ActionResult<PagedResponse<object>>> GetCourses(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = db.Courses.Include(c => c.Enrollments).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(c => c.Title.ToLower().Contains(s) || c.InstructorName.ToLower().Contains(s));
        }

        var total = await query.CountAsync();
        var courses = await query.OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new PagedResponse<object>(
            courses.Select(c => new
            {
                c.Id, c.Title, c.InstructorName, c.Category,
                c.Level, c.IsPublished, c.CreatedAt,
                EnrollmentCount = c.Enrollments.Count
            }),
            total, page, pageSize, (int)Math.Ceiling(total / (double)pageSize)));
    }

    // POST /api/admin/courses/{id}/toggle-publish
    [HttpPost("courses/{id:int}/toggle-publish")]
    public async Task<IActionResult> TogglePublish(int id)
    {
        var course = await db.Courses.FindAsync(id);
        if (course == null) return NotFound();

        course.IsPublished = !course.IsPublished;
        await db.SaveChangesAsync();
        return Ok(new { isPublished = course.IsPublished });
    }

    // DELETE /api/admin/courses/{id}
    [HttpDelete("courses/{id:int}")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var course = await db.Courses.FindAsync(id);
        if (course == null) return NotFound();

        db.Courses.Remove(course);
        await db.SaveChangesAsync();
        return NoContent();
    }
}

public record SetRoleRequest(string Role);
