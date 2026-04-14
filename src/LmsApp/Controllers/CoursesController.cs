using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[ApiController]
[Route("api/courses")]
[Authorize]
public class CoursesController(LmsDbContext db, IFileUploadService fileService, IPrerequisiteService prerequisiteService) : ControllerBase
{
    private string UserId => User.FindFirst("sub")?.Value ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";
    private string UserName => User.FindFirst("name")?.Value ?? string.Empty;

    // GET /api/courses
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<CourseResponse>>> GetAll([FromQuery] CourseFilterRequest filter)
    {
        var query = db.Courses
            .Include(c => c.Enrollments)
            .Include(c => c.Reviews)
            .Include(c => c.Modules)
            .AsQueryable();

        // Only show published courses to non-instructors/admins
        if (UserRole is not ("teacher" or "admin"))
            query = query.Where(c => c.IsPublished);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.ToLower();
            query = query.Where(c =>
                c.Title.ToLower().Contains(s) ||
                c.Description.ToLower().Contains(s) ||
                c.InstructorName.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(filter.Category) && filter.Category != "all")
            query = query.Where(c => c.Category == filter.Category);

        if (!string.IsNullOrWhiteSpace(filter.Level) && filter.Level != "all")
            query = query.Where(c => c.Level == filter.Level);

        query = filter.Sort switch
        {
            "oldest" => query.OrderBy(c => c.CreatedAt),
            "popular" => query.OrderByDescending(c => c.Enrollments.Count),
            "rating" => query.OrderByDescending(c => c.Reviews.Any() ? c.Reviews.Average(r => r.Rating) : 0),
            _ => query.OrderByDescending(c => c.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return Ok(new PagedResponse<CourseResponse>(
            items.Select(ToCourseResponse),
            total, filter.Page, filter.PageSize,
            (int)Math.Ceiling(total / (double)filter.PageSize)
        ));
    }

    // GET /api/courses/my (teacher: my courses; student: enrolled)
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<CourseResponse>>> GetMyCourses()
    {
        if (UserRole is "teacher" or "admin")
        {
            var courses = await db.Courses
                .Include(c => c.Enrollments)
                .Include(c => c.Reviews)
                .Include(c => c.Modules)
                .Where(c => c.InstructorId == UserId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return Ok(courses.Select(ToCourseResponse));
        }
        else
        {
            var courses = await db.Enrollments
                .Include(e => e.Course).ThenInclude(c => c.Enrollments)
                .Include(e => e.Course).ThenInclude(c => c.Reviews)
                .Include(e => e.Course).ThenInclude(c => c.Modules)
                .Where(e => e.UserId == UserId)
                .Select(e => e.Course)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return Ok(courses.Select(ToCourseResponse));
        }
    }

    // GET /api/courses/{id}
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CourseDetailResponse>> GetById(int id)
    {
        var isTeacher = UserRole is "teacher" or "admin";

        var course = await db.Courses
            .Include(c => c.Enrollments)
            .Include(c => c.Sections.OrderBy(s => s.Order))
                .ThenInclude(s => s.Modules.OrderBy(m => m.Order))
            .Include(c => c.Modules.OrderBy(m => m.Order))
            .Include(c => c.Assignments)
            .Include(c => c.Quizzes).ThenInclude(q => q.Questions)
            .Include(c => c.Announcements.OrderByDescending(a => a.CreatedAt).Take(5))
            .Include(c => c.Reviews.OrderByDescending(r => r.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null) return NotFound();

        var isEnrolled = !string.IsNullOrEmpty(UserId) &&
            course.Enrollments.Any(e => e.UserId == UserId);
        var enrollment = course.Enrollments.FirstOrDefault(e => e.UserId == UserId);

        // Sections — filter IsVisible untuk student
        var sections = course.Sections
            .Where(s => isTeacher || s.IsVisible)
            .Select(s => new SectionDetailDto(
                s.Id, s.CourseId, s.Title, s.Description,
                s.Order, s.IsVisible, s.CreatedAt,
                s.Modules
                    .Where(m => isTeacher || m.IsPublished)
                    .Select(m => new ModuleSummaryDto(
                        m.Id, m.Title, m.Order, m.IsPublished,
                        m.DurationMinutes, m.ContentType.ToString(), m.SectionId,
                        m.VideoEmbedId, m.VideoProvider == VideoProvider.None ? null : m.VideoProvider.ToString()))));

        // Modul tanpa section (backward compat — data lama)
        var unsectionedModules = course.Modules
            .Where(m => m.SectionId == null && (isTeacher || m.IsPublished))
            .Select(m => new ModuleSummaryDto(
                m.Id, m.Title, m.Order, m.IsPublished,
                m.DurationMinutes, m.ContentType.ToString(), null,
                m.VideoEmbedId, m.VideoProvider == VideoProvider.None ? null : m.VideoProvider.ToString()));

        return Ok(new CourseDetailResponse(
            course.Id, course.Title, course.Description, course.ThumbnailUrl,
            course.InstructorId, course.InstructorName, course.Category, course.Level,
            course.IsPublished, course.Enrollments.Count,
            course.Reviews.Any() ? course.Reviews.Average(r => r.Rating) : 0,
            course.Reviews.Count, isEnrolled,
            enrollment?.Status,
            sections,
            unsectionedModules,
            course.Assignments.Select(a => new AssignmentSummaryDto(a.Id, a.Title, a.DueDate, a.MaxScore)),
            course.Quizzes.Select(q => new QuizSummaryDto(
                q.Id, q.Title, q.TimeLimitMinutes, q.PassScore, q.DueDate, q.IsPublished, q.Questions.Count)),
            course.Announcements.Select(a => new AnnouncementDto(
                a.Id, a.Title, a.Content, a.IsPinned, a.CreatedAt)),
            course.Reviews.Take(10).Select(r => new ReviewDto(
                r.Id, r.UserId, r.UserName, r.Rating, r.Comment, r.CreatedAt)),
            course.CreatedAt
        ));
    }

    // POST /api/courses
    [HttpPost]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<CourseResponse>> Create(CourseRequest req)
    {
        var course = new Course
        {
            Title = req.Title,
            Description = req.Description,
            Category = req.Category,
            Level = req.Level,
            IsPublished = req.IsPublished,
            InstructorId = UserId,
            InstructorName = UserName
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = course.Id }, ToCourseResponse(course));
    }

    // PUT /api/courses/{id}
    [HttpPut("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<CourseResponse>> Update(int id, CourseRequest req)
    {
        var course = await db.Courses
            .Include(c => c.Enrollments).Include(c => c.Reviews).Include(c => c.Modules)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (course == null) return NotFound();

        if (UserRole != "admin" && course.InstructorId != UserId)
            return Forbid();

        course.Title = req.Title;
        course.Description = req.Description;
        course.Category = req.Category;
        course.Level = req.Level;
        course.IsPublished = req.IsPublished;
        course.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ToCourseResponse(course));
    }

    // POST /api/courses/{id}/thumbnail
    [HttpPost("{id:int}/thumbnail")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> UploadThumbnail(int id, IFormFile file)
    {
        var course = await db.Courses.FindAsync(id);
        if (course == null) return NotFound();
        if (UserRole != "admin" && course.InstructorId != UserId) return Forbid();

        if (!fileService.IsValidFile(file, [".jpg", ".jpeg", ".png", ".webp"], 5 * 1024 * 1024))
            return BadRequest(new { message = "File tidak valid. Maks 5MB." });

        if (course.ThumbnailUrl != null) fileService.Delete(course.ThumbnailUrl);
        course.ThumbnailUrl = await fileService.UploadAsync(file, "thumbnails");
        await db.SaveChangesAsync();
        return Ok(new { url = course.ThumbnailUrl });
    }

    // DELETE /api/courses/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var course = await db.Courses.FindAsync(id);
        if (course == null) return NotFound();
        if (UserRole != "admin" && course.InstructorId != UserId) return Forbid();

        db.Courses.Remove(course);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/courses/{id}/enroll
    [HttpPost("{id:int}/enroll")]
    public async Task<IActionResult> Enroll(int id)
    {
        var course = await db.Courses.FindAsync(id);
        if (course == null || !course.IsPublished) return NotFound();

        if (await db.Enrollments.AnyAsync(e => e.CourseId == id && e.UserId == UserId))
            return Conflict(new { message = "Sudah terdaftar di kursus ini." });

        // Cek prerequisite (skip untuk teacher/admin)
        if (UserRole == "student")
        {
            var prereqCheck = await prerequisiteService.CheckAsync(id, UserId);
            if (!prereqCheck.CanEnroll)
            {
                var unmet = prereqCheck.Prerequisites
                    .Where(p => !p.IsMet)
                    .Select(p => p.CourseTitle);
                return UnprocessableEntity(new
                {
                    message = "Prerequisite belum terpenuhi.",
                    unmetPrerequisites = unmet
                });
            }
        }

        var enrollment = new Enrollment
        {
            CourseId = id,
            UserId = UserId,
            UserName = UserName,
            Status = EnrollmentStatus.Active
        };
        db.Enrollments.Add(enrollment);

        db.Notifications.Add(new Notification
        {
            UserId = UserId,
            Title = "Berhasil Enroll",
            Message = $"Anda berhasil mendaftar ke kursus \"{course.Title}\".",
            Type = NotificationType.Success
        });

        await db.SaveChangesAsync();
        return Ok(new { message = "Berhasil mendaftar." });
    }

    // GET /api/courses/categories
    [AllowAnonymous]
    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<string>>> GetCategories()
    {
        var cats = await db.Courses
            .Where(c => c.Category != null && c.IsPublished)
            .Select(c => c.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
        return Ok(cats);
    }

    // POST /api/courses/{id}/reviews
    [HttpPost("{id:int}/reviews")]
    public async Task<IActionResult> SubmitReview(int id, [FromBody] SubmitReviewRequest req)
    {
        if (req.Rating < 1 || req.Rating > 5)
            return BadRequest(new { message = "Rating harus antara 1 dan 5." });

        if (!await db.Enrollments.AnyAsync(e => e.CourseId == id && e.UserId == UserId))
            return Forbid();

        var existing = await db.CourseReviews.FirstOrDefaultAsync(r => r.CourseId == id && r.UserId == UserId);
        if (existing != null)
        {
            existing.Rating = req.Rating;
            existing.Comment = req.Comment;
        }
        else
        {
            db.CourseReviews.Add(new CourseReview
            {
                CourseId = id,
                UserId = UserId,
                UserName = UserName,
                Rating = req.Rating,
                Comment = req.Comment
            });
        }
        await db.SaveChangesAsync();
        return Ok(new { message = "Review berhasil disimpan." });
    }

    private static CourseResponse ToCourseResponse(Course c) => new(
        c.Id, c.Title, c.Description, c.ThumbnailUrl,
        c.InstructorId, c.InstructorName, c.Category, c.Level,
        c.IsPublished,
        c.Enrollments?.Count ?? 0,
        c.Reviews?.Any() == true ? c.Reviews.Average(r => r.Rating) : 0,
        c.Reviews?.Count ?? 0,
        c.Modules?.Count ?? 0,
        c.CreatedAt, c.UpdatedAt
    );
}

public record SubmitReviewRequest(int Rating, string? Comment);
