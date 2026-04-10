using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[ApiController]
[Route("api/courses/{courseId:int}/modules")]
[Authorize]
public class ModulesController(
    LmsDbContext db,
    IFileUploadService fileService,
    ICompletionService completionService,
    IActivityLogService activityLogService) : ControllerBase
{
    private string UserId   => User.FindFirst("sub")?.Value  ?? string.Empty;
    private string UserName => User.FindFirst("name")?.Value ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ModuleResponse>>> GetAll(int courseId)
    {
        var isTeacher = await IsTeacherOrAdmin(courseId);
        var isEnrolled = await db.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserId == UserId);

        if (!isTeacher && !isEnrolled) return Forbid();

        var modules = await db.CourseModules
            .Include(m => m.Attachments)
            .Where(m => m.CourseId == courseId && (isTeacher || m.IsPublished))
            .OrderBy(m => m.Order)
            .ToListAsync();

        return Ok(modules.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ModuleResponse>> GetById(int courseId, int id)
    {
        var isTeacher = await IsTeacherOrAdmin(courseId);
        var isEnrolled = await db.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserId == UserId);
        if (!isTeacher && !isEnrolled) return Forbid();

        var module = await db.CourseModules
            .Include(m => m.Attachments)
            .FirstOrDefaultAsync(m => m.Id == id && m.CourseId == courseId);
        if (module == null) return NotFound();
        if (!isTeacher && !module.IsPublished) return Forbid();

        // Log activity (fire-and-forget)
        _ = activityLogService.LogAsync(UserId, UserName, "view_module", "Module", module.Id, module.Title, module.CourseId);

        // Mark progress
        if (isEnrolled)
        {
            var progress = await db.CourseProgresses
                .FirstOrDefaultAsync(p => p.CourseId == courseId && p.UserId == UserId);
            if (progress == null)
            {
                progress = new CourseProgress { CourseId = courseId, UserId = UserId };
                db.CourseProgresses.Add(progress);
                await db.SaveChangesAsync();
            }

            if (!await db.ModuleProgresses.AnyAsync(mp => mp.ModuleId == id && mp.UserId == UserId))
            {
                db.ModuleProgresses.Add(new ModuleProgress
                {
                    CourseProgressId = progress.Id,
                    ModuleId = id,
                    UserId = UserId,
                    CompletedAt = DateTime.UtcNow
                });
                var totalModules = await db.CourseModules.CountAsync(m => m.CourseId == courseId && m.IsPublished);
                var doneModules = await db.ModuleProgresses.CountAsync(mp => mp.UserId == UserId && mp.CourseProgress.CourseId == courseId);
                progress.CompletedModules = doneModules + 1;
                progress.TotalModules     = totalModules;
                progress.LastAccessedAt   = DateTime.UtcNow;
                await db.SaveChangesAsync();

                // Setelah progress disimpan, cek apakah course sudah selesai
                // TryIssueCertificateAsync bersifat idempotent — aman dipanggil setiap kali
                await completionService.TryIssueCertificateAsync(courseId, UserId, UserName);
            }
        }

        return Ok(ToDto(module));
    }

    [HttpPost]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<ModuleResponse>> Create(int courseId, ModuleRequest req)
    {
        if (!await IsTeacherOrAdmin(courseId)) return Forbid();

        var (embedId, provider) = VideoService.Parse(req.VideoUrl);
        var contentType = DetermineContentType(req.Content, req.VideoUrl);

        // Jika SectionId diberikan, pastikan section itu milik course yang sama
        if (req.SectionId.HasValue)
        {
            var sectionExists = await db.CourseSections
                .AnyAsync(s => s.Id == req.SectionId.Value && s.CourseId == courseId);
            if (!sectionExists)
                return BadRequest(new { message = $"Section {req.SectionId} tidak ditemukan di course ini." });
        }

        var module = new CourseModule
        {
            CourseId = courseId,
            SectionId = req.SectionId,
            Title = req.Title,
            Content = req.Content,
            VideoUrl = req.VideoUrl,
            VideoEmbedId = embedId,
            VideoProvider = provider,
            ContentType = contentType,
            Order = req.Order,
            IsPublished = req.IsPublished,
            DurationMinutes = req.DurationMinutes
        };
        db.CourseModules.Add(module);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { courseId, id = module.Id }, ToDto(module));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<ModuleResponse>> Update(int courseId, int id, ModuleRequest req)
    {
        if (!await IsTeacherOrAdmin(courseId)) return Forbid();

        var module = await db.CourseModules.Include(m => m.Attachments)
            .FirstOrDefaultAsync(m => m.Id == id && m.CourseId == courseId);
        if (module == null) return NotFound();

        var (embedId, provider) = VideoService.Parse(req.VideoUrl);

        // Validasi SectionId jika diubah
        if (req.SectionId.HasValue)
        {
            var sectionExists = await db.CourseSections
                .AnyAsync(s => s.Id == req.SectionId.Value && s.CourseId == courseId);
            if (!sectionExists)
                return BadRequest(new { message = $"Section {req.SectionId} tidak ditemukan di course ini." });
        }

        module.SectionId = req.SectionId;
        module.Title = req.Title;
        module.Content = req.Content;
        module.VideoUrl = req.VideoUrl;
        module.VideoEmbedId = embedId;
        module.VideoProvider = provider;
        module.ContentType = DetermineContentType(req.Content, req.VideoUrl);
        module.Order = req.Order;
        module.IsPublished = req.IsPublished;
        module.DurationMinutes = req.DurationMinutes;
        await db.SaveChangesAsync();
        return Ok(ToDto(module));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> Delete(int courseId, int id)
    {
        if (!await IsTeacherOrAdmin(courseId)) return Forbid();

        var module = await db.CourseModules.FindAsync(id);
        if (module == null || module.CourseId != courseId) return NotFound();

        db.CourseModules.Remove(module);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("reorder")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> Reorder(int courseId, ReorderRequest req)
    {
        if (!await IsTeacherOrAdmin(courseId)) return Forbid();

        var ids = req.Items.Select(i => i.Id).ToList();
        var modules = await db.CourseModules.Where(m => ids.Contains(m.Id) && m.CourseId == courseId).ToListAsync();

        foreach (var item in req.Items)
        {
            var module = modules.FirstOrDefault(m => m.Id == item.Id);
            if (module != null) module.Order = item.Order;
        }
        await db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/courses/{courseId}/modules/{id}/attachments
    [HttpPost("{id:int}/attachments")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> UploadAttachment(int courseId, int id, IFormFile file)
    {
        if (!await IsTeacherOrAdmin(courseId)) return Forbid();

        var module = await db.CourseModules.FindAsync(id);
        if (module == null || module.CourseId != courseId) return NotFound();

        if (!fileService.IsValidFile(file, [".pdf", ".doc", ".docx", ".ppt", ".pptx", ".zip", ".mp4", ".mp3"], 100 * 1024 * 1024))
            return BadRequest(new { message = "File tidak valid atau melebihi batas ukuran." });

        var url = await fileService.UploadAsync(file, "modules");
        var attachment = new ModuleAttachment
        {
            ModuleId = id,
            FileName = file.FileName,
            FileUrl = url,
            FileSize = file.Length,
            FileType = Path.GetExtension(file.FileName).TrimStart('.').ToLower()
        };
        db.ModuleAttachments.Add(attachment);
        await db.SaveChangesAsync();
        return Ok(new AttachmentDto(attachment.Id, attachment.FileName, attachment.FileUrl, attachment.FileSize, attachment.FileType));
    }

    // DELETE /api/courses/{courseId}/modules/{id}/attachments/{attId}
    [HttpDelete("{id:int}/attachments/{attId:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> DeleteAttachment(int courseId, int id, int attId)
    {
        if (!await IsTeacherOrAdmin(courseId)) return Forbid();

        var att = await db.ModuleAttachments.FindAsync(attId);
        if (att == null || att.ModuleId != id) return NotFound();

        fileService.Delete(att.FileUrl);
        db.ModuleAttachments.Remove(att);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> IsTeacherOrAdmin(int courseId)
    {
        if (UserRole == "admin") return true;
        if (UserRole == "teacher")
            return await db.Courses.AnyAsync(c => c.Id == courseId && c.InstructorId == UserId);
        return false;
    }

    private static ModuleContentType DetermineContentType(string? content, string? videoUrl)
    {
        bool hasText = !string.IsNullOrWhiteSpace(content);
        bool hasVideo = !string.IsNullOrWhiteSpace(videoUrl);
        return (hasText, hasVideo) switch
        {
            (true, true) => ModuleContentType.Mixed,
            (false, true) => ModuleContentType.Video,
            _ => ModuleContentType.Text
        };
    }

    private static ModuleResponse ToDto(CourseModule m) => new(
        m.Id, m.CourseId, m.SectionId, m.Title, m.Content, m.VideoUrl, m.VideoEmbedId,
        m.VideoProvider.ToString(), m.ContentType.ToString(),
        m.Order, m.IsPublished, m.DurationMinutes,
        m.Attachments.Select(a => new AttachmentDto(a.Id, a.FileName, a.FileUrl, a.FileSize, a.FileType)).ToList()
    );
}
