using LmsApp.DTOs;
using LmsApp.Models;

namespace LmsApp.Services;

public interface ICourseSectionService
{
    // ── Query ─────────────────────────────────────────────────────────────────

    /// <summary>Ambil semua section (+ modul di dalamnya) untuk satu course.</summary>
    Task<IEnumerable<SectionDetailDto>> GetSectionsAsync(int courseId, bool includeHidden);

    /// <summary>Ambil satu section beserta modulnya.</summary>
    Task<SectionDetailDto?> GetByIdAsync(int sectionId, bool includeHidden);

    // ── Mutasi Section ────────────────────────────────────────────────────────

    /// <summary>Buat section baru. Melempar UnauthorizedAccessException jika user bukan owner/admin.</summary>
    Task<CourseSection> CreateAsync(int courseId, CreateSectionRequest req, string userId, bool isAdmin);

    /// <summary>Update judul/deskripsi/visibilitas section.</summary>
    Task UpdateAsync(int sectionId, UpdateSectionRequest req, string userId, bool isAdmin);

    /// <summary>Hapus section. Modul di dalamnya menjadi unsectioned (SectionId = null).</summary>
    Task DeleteAsync(int sectionId, string userId, bool isAdmin);

    /// <summary>Atur ulang urutan section dalam course.</summary>
    Task ReorderAsync(int courseId, List<ReorderItem> items, string userId, bool isAdmin);

    // ── Mutasi Modul ──────────────────────────────────────────────────────────

    /// <summary>Pindahkan modul ke section lain (atau jadi unsectioned jika sectionId = null).</summary>
    Task MoveModuleAsync(int moduleId, int? sectionId, string userId, bool isAdmin);
}
