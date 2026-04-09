using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

/// <summary>
/// File Manager / Resource Module — resource files di level course.
///
/// Routes:
///   GET    api/courses/{courseId}/resources            → list resources
///   GET    api/courses/{courseId}/resources/{id}       → detail satu resource
///   POST   api/courses/{courseId}/resources            → upload file baru (multipart)
///   PUT    api/courses/{courseId}/resources/{id}       → update metadata
///   DELETE api/courses/{courseId}/resources/{id}       → hapus resource + file
///   POST   api/courses/{courseId}/resources/{id}/download → increment counter + URL
/// </summary>
[ApiController]
[Route("api/courses/{courseId:int}/resources")]
[Authorize]
public class CourseResourcesController(
    LmsDbContext db, IFileUploadService fileService) : ControllerBase
{
    private string UserId       => User.FindFirst("sub")?.Value  ?? string.Empty;
    private string UserRole     => User.FindFirst("role")?.Value ?? "student";
    private string UserName     => User.FindFirst("name")?.Value ?? string.Empty;
    private bool   IsTeacherOrAdmin => UserRole is "teacher" or "admin";

    private static readonly string[] AllowedExtensions =
        [".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx",
         ".zip", ".rar", ".txt", ".mp4", ".webm", ".mp3",
         ".png", ".jpg", ".jpeg", ".gif", ".svg"];

    private const long MaxFileSize = 100 * 1024 * 1024; // 100 MB

    // ── List ──────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourseResourceDto>>> GetAll(int courseId)
    {
        if (!await CanAccessAsync(courseId)) return Forbid();

        var query = db.CourseResources.Where(r => r.CourseId == courseId);

        // Student hanya lihat yang visible
        if (!IsTeacherOrAdmin)
            query = query.Where(r => r.IsVisible);

        var resources = await query
            .OrderBy(r => r.Order)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(resources.Select(ToDto));
    }

    // ── Detail ────────────────────────────────────────────────────────────────

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CourseResourceDto>> GetById(int courseId, int id)
    {
        if (!await CanAccessAsync(courseId)) return Forbid();

        var resource = await db.CourseResources
            .FirstOrDefaultAsync(r => r.Id == id && r.CourseId == courseId);

        if (resource == null) return NotFound();
        if (!resource.IsVisible && !IsTeacherOrAdmin) return Forbid();

        return Ok(ToDto(resource));
    }

    // ── Upload ────────────────────────────────────────────────────────────────

    /// <summary>
    /// POST api/courses/{courseId}/resources
    /// Form fields: file (required), title (optional, defaults to filename), description, order
    /// Accepted types: PDF, DOC, PPT, XLS, ZIP, MP4, MP3, images (max 100 MB)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "teacher,admin")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("upload")]
    public async Task<ActionResult<CourseResourceDto>> Upload(
        int courseId,
        IFormFile file,
        [FromForm] string? title,
        [FromForm] string? description,
        [FromForm] int order = 0)
    {
        var course = await db.Courses.FindAsync(courseId);
        if (course == null) return NotFound(new { message = "Course tidak ditemukan." });

        if (UserRole != "admin" && course.InstructorId != UserId)
            return Forbid();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "File tidak boleh kosong." });

        if (!fileService.IsValidFile(file, AllowedExtensions, MaxFileSize))
            return BadRequest(new { message = $"File tidak valid. Tipe yang diizinkan: {string.Join(", ", AllowedExtensions)}. Ukuran maks: 100 MB." });

        var fileUrl  = await fileService.UploadAsync(file, "resources");
        var fileType = DetectFileType(Path.GetExtension(file.FileName));

        var resource = new CourseResource
        {
            CourseId       = courseId,
            Title          = !string.IsNullOrWhiteSpace(title) ? title : Path.GetFileNameWithoutExtension(file.FileName),
            Description    = description,
            FileName       = file.FileName,
            FileUrl        = fileUrl,
            FileType       = fileType,
            FileSize       = file.Length,
            Order          = order,
            UploadedBy     = UserId,
            UploadedByName = UserName
        };
        db.CourseResources.Add(resource);

        try
        {
            await db.SaveChangesAsync();
        }
        catch
        {
            // Hapus file yang sudah terupload jika penyimpanan DB gagal
            fileService.Delete(fileUrl);
            throw;
        }

        return CreatedAtAction(nameof(GetById), new { courseId, id = resource.Id }, ToDto(resource));
    }

    // ── Update Metadata ───────────────────────────────────────────────────────

    [HttpPut("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<CourseResourceDto>> Update(
        int courseId, int id, UpdateResourceRequest req)
    {
        var resource = await db.CourseResources
            .FirstOrDefaultAsync(r => r.Id == id && r.CourseId == courseId);
        if (resource == null) return NotFound();

        var course = await db.Courses.FindAsync(courseId);
        if (UserRole != "admin" && course!.InstructorId != UserId) return Forbid();

        resource.Title       = req.Title;
        resource.Description = req.Description;
        resource.IsVisible   = req.IsVisible;
        resource.Order       = req.Order;
        await db.SaveChangesAsync();

        return Ok(ToDto(resource));
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> Delete(int courseId, int id)
    {
        var resource = await db.CourseResources
            .FirstOrDefaultAsync(r => r.Id == id && r.CourseId == courseId);
        if (resource == null) return NotFound();

        var course = await db.Courses.FindAsync(courseId);
        if (UserRole != "admin" && course!.InstructorId != UserId) return Forbid();

        fileService.Delete(resource.FileUrl);
        db.CourseResources.Remove(resource);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Download (track) ──────────────────────────────────────────────────────

    /// <summary>
    /// POST api/courses/{courseId}/resources/{id}/download
    /// Increment download counter dan kembalikan URL file.
    /// </summary>
    [HttpPost("{id:int}/download")]
    public async Task<IActionResult> Download(int courseId, int id)
    {
        if (!await CanAccessAsync(courseId)) return Forbid();

        var resource = await db.CourseResources
            .FirstOrDefaultAsync(r => r.Id == id && r.CourseId == courseId);

        if (resource == null) return NotFound();
        if (!resource.IsVisible && !IsTeacherOrAdmin) return Forbid();

        // Atomic increment — cegah race condition jika banyak user download bersamaan
        await db.CourseResources
            .Where(r => r.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.DownloadCount, r => r.DownloadCount + 1));

        return Ok(new { fileUrl = resource.FileUrl, fileName = resource.FileName });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<bool> CanAccessAsync(int courseId)
    {
        if (IsTeacherOrAdmin) return true;
        return await db.Enrollments
            .AnyAsync(e => e.CourseId == courseId && e.UserId == UserId);
    }

    private static string DetectFileType(string ext) => ext.ToLowerInvariant() switch
    {
        ".pdf"                         => "pdf",
        ".mp4" or ".webm" or ".avi"    => "video",
        ".mp3" or ".wav" or ".ogg"     => "audio",
        ".doc" or ".docx"              => "doc",
        ".ppt" or ".pptx"              => "presentation",
        ".xls" or ".xlsx"              => "spreadsheet",
        ".zip" or ".rar"               => "archive",
        ".png" or ".jpg" or
        ".jpeg" or ".gif" or ".svg"    => "image",
        _                              => "other"
    };

    private static string FormatFileSize(long bytes)
    {
        if (bytes >= 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        if (bytes >= 1024)        return $"{bytes / 1024.0:F0} KB";
        return $"{bytes} B";
    }

    private static CourseResourceDto ToDto(CourseResource r) => new(
        r.Id, r.CourseId, r.Title, r.Description,
        r.FileName, r.FileUrl, r.FileType, r.FileSize,
        FormatFileSize(r.FileSize),
        r.IsVisible, r.Order, r.DownloadCount,
        r.UploadedByName, r.CreatedAt
    );
}
