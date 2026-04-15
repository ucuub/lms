using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Services;

public class CourseSectionService(LmsDbContext db) : ICourseSectionService
{
    // ── Query ─────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<SectionDetailDto>> GetSectionsAsync(int courseId, bool includeHidden)
    {
        var sections = await db.CourseSections
            .Where(s => s.CourseId == courseId && (includeHidden || s.IsVisible))
            .OrderBy(s => s.Order)
            .Include(s => s.Modules)
            .ToListAsync();

        return sections.Select(s => new SectionDetailDto(
            s.Id, s.CourseId, s.Title, s.Description,
            s.Order, s.IsVisible, s.CreatedAt,
            s.Modules
                .Where(m => includeHidden || m.IsPublished)
                .OrderBy(m => m.Order)
                .Select(m => new ModuleSummaryDto(
                    m.Id, m.Title, m.Order, m.IsPublished,
                    m.DurationMinutes, m.ContentType.ToString(), m.SectionId))
                .ToList()));
    }

    public async Task<SectionDetailDto?> GetByIdAsync(int sectionId, bool includeHidden)
    {
        var s = await db.CourseSections
            .Where(s => s.Id == sectionId && (includeHidden || s.IsVisible))
            .Include(s => s.Modules)
            .FirstOrDefaultAsync();

        if (s == null) return null;
        return new SectionDetailDto(
            s.Id, s.CourseId, s.Title, s.Description,
            s.Order, s.IsVisible, s.CreatedAt,
            s.Modules
                .Where(m => includeHidden || m.IsPublished)
                .OrderBy(m => m.Order)
                .Select(m => new ModuleSummaryDto(
                    m.Id, m.Title, m.Order, m.IsPublished,
                    m.DurationMinutes, m.ContentType.ToString(), m.SectionId))
                .ToList());
    }

    // ── Mutasi Section ────────────────────────────────────────────────────────

    public async Task<CourseSection> CreateAsync(
        int courseId, CreateSectionRequest req, string userId, bool isAdmin)
    {
        await GuardCourseOwnerAsync(courseId, userId, isAdmin);

        var section = new CourseSection
        {
            CourseId = courseId,
            Title = req.Title,
            Description = req.Description,
            Order = req.Order,
            IsVisible = req.IsVisible
        };

        db.CourseSections.Add(section);
        await db.SaveChangesAsync();
        return section;
    }

    public async Task UpdateAsync(
        int sectionId, UpdateSectionRequest req, string userId, bool isAdmin)
    {
        var section = await db.CourseSections
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == sectionId)
            ?? throw new KeyNotFoundException($"Section {sectionId} tidak ditemukan.");

        await GuardCourseOwnerAsync(section.CourseId, userId, isAdmin);

        section.Title = req.Title;
        section.Description = req.Description;
        section.Order = req.Order;
        section.IsVisible = req.IsVisible;

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int sectionId, string userId, bool isAdmin)
    {
        var section = await db.CourseSections
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == sectionId)
            ?? throw new KeyNotFoundException($"Section {sectionId} tidak ditemukan.");

        await GuardCourseOwnerAsync(section.CourseId, userId, isAdmin);

        // EF akan set SectionId = null pada semua modul (karena OnDelete.SetNull di DbContext)
        db.CourseSections.Remove(section);
        await db.SaveChangesAsync();
    }

    public async Task ReorderAsync(
        int courseId, List<ReorderItem> items, string userId, bool isAdmin)
    {
        await GuardCourseOwnerAsync(courseId, userId, isAdmin);

        var ids = items.Select(i => i.Id).ToList();
        var sections = await db.CourseSections
            .Where(s => s.CourseId == courseId && ids.Contains(s.Id))
            .ToListAsync();

        foreach (var item in items)
        {
            var section = sections.FirstOrDefault(s => s.Id == item.Id);
            if (section != null) section.Order = item.Order;
        }

        await db.SaveChangesAsync();
    }

    // ── Mutasi Modul ──────────────────────────────────────────────────────────

    public async Task MoveModuleAsync(int moduleId, int? sectionId, string userId, bool isAdmin)
    {
        var module = await db.CourseModules
            .Include(m => m.Course)
            .FirstOrDefaultAsync(m => m.Id == moduleId)
            ?? throw new KeyNotFoundException($"Module {moduleId} tidak ditemukan.");

        await GuardCourseOwnerAsync(module.CourseId, userId, isAdmin);

        // Jika sectionId diberikan, pastikan section itu milik course yang sama
        if (sectionId.HasValue)
        {
            var sectionExists = await db.CourseSections
                .AnyAsync(s => s.Id == sectionId.Value && s.CourseId == module.CourseId);

            if (!sectionExists)
                throw new InvalidOperationException(
                    $"Section {sectionId} tidak ditemukan di course {module.CourseId}.");
        }

        module.SectionId = sectionId; // null = unsectioned
        await db.SaveChangesAsync();
    }

    // ── Guard Helper ──────────────────────────────────────────────────────────

    /// <summary>
    /// Pastikan user adalah instructor course atau admin.
    /// Melempar UnauthorizedAccessException jika tidak.
    /// </summary>
    private async Task GuardCourseOwnerAsync(int courseId, string userId, bool isAdmin)
    {
        if (isAdmin) return; // admin bisa semua

        var isOwner = await db.Courses
            .AnyAsync(c => c.Id == courseId && c.InstructorId == userId);

        if (!isOwner)
            throw new UnauthorizedAccessException(
                "Hanya instructor atau admin yang bisa mengelola section course ini.");
    }
}
