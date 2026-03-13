using LmsApp.Data;
using LmsApp.Models;
using LmsApp.Services;
using LmsApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[Authorize]
public class ModuleController(LmsDbContext db, IFileUploadService fileUpload) : Controller
{
    // GET: /Module/View/5  — peserta membaca modul
    public async Task<IActionResult> View(int id)
    {
        var module = await db.CourseModules
            .Include(m => m.Course)
            .Include(m => m.Attachments)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (module is null || !module.IsPublished) return NotFound();

        var userId = User.FindFirst("sub")?.Value ?? string.Empty;

        // Pastikan peserta sudah enrolled
        var isEnrolled = await db.Enrollments
            .AnyAsync(e => e.CourseId == module.CourseId && e.UserId == userId);

        var isInstructor = User.IsInRole("instructor") || User.IsInRole("admin");
        if (!isEnrolled && !isInstructor) return Forbid();

        // Ambil modul prev/next untuk navigasi
        var allModules = await db.CourseModules
            .Where(m => m.CourseId == module.CourseId && m.IsPublished)
            .OrderBy(m => m.Order)
            .Select(m => new { m.Id, m.Title, m.Order })
            .ToListAsync();

        var currentIndex = allModules.FindIndex(m => m.Id == id);
        ViewBag.PrevModule = currentIndex > 0 ? allModules[currentIndex - 1] : null;
        ViewBag.NextModule = currentIndex < allModules.Count - 1 ? allModules[currentIndex + 1] : null;
        ViewBag.IsCompleted = await db.ModuleProgresses
            .AnyAsync(mp => mp.ModuleId == id && mp.UserId == userId);

        ViewBag.EmbedUrl = VideoService.GetEmbedUrl(module.VideoEmbedId, module.VideoProvider);
        return View(module);
    }

    // GET: /Module/Manage/courseId  — instruktur kelola modul
    [Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> Manage(int courseId)
    {
        var course = await db.Courses
            .Include(c => c.Modules.OrderBy(m => m.Order))
            .ThenInclude(m => m.Attachments)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course is null) return NotFound();
        return View(course);
    }

    // GET: /Module/Create?courseId=5
    [Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> Create(int courseId)
    {
        var course = await db.Courses.FindAsync(courseId);
        if (course is null) return NotFound();

        ViewBag.Course = course;
        return View(new ModuleCreateViewModel { CourseId = courseId });
    }

    // POST: /Module/Create
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> Create(ModuleCreateViewModel vm, List<IFormFile>? attachments)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Course = await db.Courses.FindAsync(vm.CourseId);
            return View(vm);
        }

        var order = await db.CourseModules.CountAsync(m => m.CourseId == vm.CourseId) + 1;

        // Parse video URL
        var (embedId, provider) = VideoService.Parse(vm.VideoUrl);

        var module = new CourseModule
        {
            CourseId = vm.CourseId,
            Title = vm.Title,
            Content = vm.Content,
            VideoUrl = vm.VideoUrl,
            VideoEmbedId = embedId,
            VideoProvider = provider,
            ContentType = DetermineContentType(vm.Content, vm.VideoUrl),
            DurationMinutes = vm.DurationMinutes,
            IsPublished = vm.IsPublished,
            Order = order
        };

        db.CourseModules.Add(module);
        await db.SaveChangesAsync();

        // Upload attachments
        if (attachments?.Count > 0)
            await SaveAttachments(module.Id, attachments);

        TempData["Success"] = "Modul berhasil ditambahkan!";
        return RedirectToAction(nameof(Manage), new { courseId = vm.CourseId });
    }

    // GET: /Module/Edit/5
    [Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var module = await db.CourseModules
            .Include(m => m.Course)
            .Include(m => m.Attachments)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (module is null) return NotFound();

        var vm = new ModuleCreateViewModel
        {
            CourseId = module.CourseId,
            Title = module.Title,
            Content = module.Content,
            VideoUrl = module.VideoUrl,
            DurationMinutes = module.DurationMinutes,
            IsPublished = module.IsPublished
        };

        ViewBag.Module = module;
        ViewBag.Course = module.Course;
        return View(vm);
    }

    // POST: /Module/Edit/5
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> Edit(int id, ModuleCreateViewModel vm, List<IFormFile>? attachments)
    {
        var module = await db.CourseModules.Include(m => m.Course).FirstOrDefaultAsync(m => m.Id == id);
        if (module is null) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.Module = module;
            ViewBag.Course = module.Course;
            return View(vm);
        }

        var (embedId, provider) = VideoService.Parse(vm.VideoUrl);

        module.Title = vm.Title;
        module.Content = vm.Content;
        module.VideoUrl = vm.VideoUrl;
        module.VideoEmbedId = embedId;
        module.VideoProvider = provider;
        module.ContentType = DetermineContentType(vm.Content, vm.VideoUrl);
        module.DurationMinutes = vm.DurationMinutes;
        module.IsPublished = vm.IsPublished;
        module.UpdatedAt = DateTime.UtcNow;

        if (attachments?.Count > 0)
            await SaveAttachments(module.Id, attachments);

        await db.SaveChangesAsync();
        TempData["Success"] = "Modul berhasil diperbarui!";
        return RedirectToAction(nameof(Manage), new { courseId = module.CourseId });
    }

    // POST: /Module/Delete/5
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var module = await db.CourseModules
            .Include(m => m.Attachments)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (module is null) return NotFound();

        // Delete attachment files
        foreach (var att in module.Attachments)
            fileUpload.Delete(att.FileUrl);

        var courseId = module.CourseId;
        db.CourseModules.Remove(module);
        await db.SaveChangesAsync();

        // Re-order remaining modules
        var remaining = await db.CourseModules
            .Where(m => m.CourseId == courseId)
            .OrderBy(m => m.Order)
            .ToListAsync();

        for (int i = 0; i < remaining.Count; i++)
            remaining[i].Order = i + 1;

        await db.SaveChangesAsync();
        TempData["Success"] = "Modul dihapus.";
        return RedirectToAction(nameof(Manage), new { courseId });
    }

    // POST: /Module/Reorder  — AJAX drag-drop
    [HttpPost, Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> Reorder([FromBody] List<int> orderedIds)
    {
        for (int i = 0; i < orderedIds.Count; i++)
        {
            var m = await db.CourseModules.FindAsync(orderedIds[i]);
            if (m is not null) m.Order = i + 1;
        }
        await db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // POST: /Module/DeleteAttachment/5
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> DeleteAttachment(int id)
    {
        var att = await db.ModuleAttachments.Include(a => a.Module).FirstOrDefaultAsync(a => a.Id == id);
        if (att is null) return NotFound();

        fileUpload.Delete(att.FileUrl);
        var moduleId = att.ModuleId;
        var courseId = att.Module.CourseId;
        db.ModuleAttachments.Remove(att);
        await db.SaveChangesAsync();

        TempData["Success"] = "Lampiran dihapus.";
        return RedirectToAction(nameof(Edit), new { id = moduleId });
    }

    // ── Helpers ──────────────────────────────────────────────────────
    private static ModuleContentType DetermineContentType(string? content, string? videoUrl)
    {
        bool hasContent = !string.IsNullOrWhiteSpace(content) && content != "<p><br></p>";
        bool hasVideo = !string.IsNullOrWhiteSpace(videoUrl);
        return (hasContent, hasVideo) switch
        {
            (true, true) => ModuleContentType.Mixed,
            (false, true) => ModuleContentType.Video,
            _ => ModuleContentType.Text
        };
    }

    private async Task SaveAttachments(int moduleId, List<IFormFile> files)
    {
        foreach (var file in files.Where(f => f.Length > 0))
        {
            if (!fileUpload.IsValidFile(file, [".pdf", ".doc", ".docx", ".zip", ".pptx", ".xlsx", ".png", ".jpg"], 50 * 1024 * 1024))
                continue;

            var url = await fileUpload.UploadAsync(file, "modules");
            db.ModuleAttachments.Add(new ModuleAttachment
            {
                ModuleId = moduleId,
                FileName = file.FileName,
                FileUrl = url,
                FileType = Path.GetExtension(file.FileName).ToLowerInvariant(),
                FileSizeBytes = file.Length
            });
        }
        await db.SaveChangesAsync();
    }
}
