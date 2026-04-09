using System.ComponentModel.DataAnnotations;

namespace LmsApp.DTOs;

// ── Requests ──────────────────────────────────────────────────────────────────

public record AddPrerequisiteRequest(
    [Required] int PrerequisiteCourseId
);

// ── Responses ─────────────────────────────────────────────────────────────────

/// <summary>
/// Satu prerequisite course beserta status apakah user sudah memenuhinya.
/// </summary>
public record PrerequisiteDto(
    int PrerequisiteId,         // ID row CoursePrerequisite
    int CourseId,               // course yang jadi syarat
    string CourseTitle,
    string Level,
    string? ThumbnailUrl,
    bool IsMet                  // true = user sudah selesaikan course ini
);

/// <summary>
/// Hasil cek prerequisite untuk enrollment.
/// </summary>
public record PrerequisiteCheckDto(
    bool CanEnroll,             // true = semua prerequisite terpenuhi
    IEnumerable<PrerequisiteDto> Prerequisites
);
