using System.ComponentModel.DataAnnotations;

namespace LmsApp.DTOs;

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record CreateSectionRequest(
    [Required, MaxLength(200)] string Title,
    string? Description = null,
    int Order = 0,
    bool IsVisible = true
);

public record UpdateSectionRequest(
    [Required, MaxLength(200)] string Title,
    string? Description,
    int Order,
    bool IsVisible
);

// Pisahkan dari ReorderRequest modul agar tidak ambigu di Swagger
public record ReorderSectionsRequest(List<ReorderItem> Items);

// Request untuk pindahkan modul ke section lain
// SectionId = null → modul jadi "unsectioned"
public record MoveModuleRequest(int? SectionId);

// ── Response DTOs ─────────────────────────────────────────────────────────────

/// <summary>
/// Ringkasan section tanpa modul (untuk list cepat).
/// </summary>
public record SectionSummaryDto(
    int Id,
    int CourseId,
    string Title,
    string? Description,
    int Order,
    bool IsVisible,
    int ModuleCount,
    DateTime CreatedAt
);

/// <summary>
/// Section lengkap dengan modul-modul di dalamnya.
/// Digunakan di course detail dan GET /sections endpoint.
/// </summary>
public record SectionDetailDto(
    int Id,
    int CourseId,
    string Title,
    string? Description,
    int Order,
    bool IsVisible,
    DateTime CreatedAt,
    IEnumerable<ModuleSummaryDto> Modules   // modul dalam section ini, ordered by Order
);
