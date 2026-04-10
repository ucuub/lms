using LmsApp.Data;
using LmsApp.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[ApiController]
[Route("api/search")]
[Authorize]
public class SearchController(LmsDbContext db) : ControllerBase
{
    private string UserId   => User.FindFirst("sub")?.Value  ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";
    private bool IsTeacherOrAdmin => UserRole is "teacher" or "admin";

    // GET /api/search?q=keyword&limit=5
    [HttpGet]
    public async Task<ActionResult<SearchResultDto>> Search(
        [FromQuery] string q = "", [FromQuery] int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new SearchResultDto([], [], []));

        var term = q.Trim().ToLower();

        // Search courses (only published for students)
        var courseQuery = db.Courses
            .Include(c => c.Enrollments)
            .Where(c => IsTeacherOrAdmin || c.IsPublished)
            .Where(c => c.Title.ToLower().Contains(term) || c.Description.ToLower().Contains(term));

        var courses = await courseQuery.Take(limit).Select(c => new CourseSearchItem(
            c.Id, c.Title, c.Description, c.ThumbnailUrl,
            c.Category ?? "", c.Level ?? "",
            c.Enrollments.Count)).ToListAsync();

        // Search modules (only published, only enrolled courses for students)
        var moduleQuery = db.CourseModules
            .Include(m => m.Course)
            .Where(m => m.IsPublished && (IsTeacherOrAdmin || m.Course.IsPublished))
            .Where(m => m.Title.ToLower().Contains(term));

        if (!IsTeacherOrAdmin)
        {
            var enrolledCourseIds = await db.Enrollments
                .Where(e => e.UserId == UserId)
                .Select(e => e.CourseId)
                .ToListAsync();
            moduleQuery = moduleQuery.Where(m => enrolledCourseIds.Contains(m.CourseId));
        }

        var modules = await moduleQuery.Take(limit).Select(m => new ModuleSearchItem(
            m.Id, m.CourseId, m.Course.Title, m.Title, m.ContentType.ToString())).ToListAsync();

        // Search forum posts (non-deleted threads/posts)
        var forumQuery = db.ForumPosts
            .Include(p => p.Course)
            .Where(p => !p.IsDeleted)
            .Where(p => (p.Title != null && p.Title.ToLower().Contains(term)) || p.Body.ToLower().Contains(term));

        if (!IsTeacherOrAdmin)
        {
            var enrolledCourseIds = await db.Enrollments
                .Where(e => e.UserId == UserId)
                .Select(e => e.CourseId)
                .ToListAsync();
            forumQuery = forumQuery.Where(p => enrolledCourseIds.Contains(p.CourseId));
        }

        var forumPosts = await forumQuery
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .Select(p => new ForumSearchItem(
                p.Id, p.CourseId, p.Course.Title, p.Title, p.Body, p.UserName, p.CreatedAt))
            .ToListAsync();

        return Ok(new SearchResultDto(courses, modules, forumPosts));
    }
}
