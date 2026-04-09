using System.ComponentModel.DataAnnotations;

namespace LmsApp.DTOs;

// ── Requests ──────────────────────────────────────────────────────────────────

public record UpdateResourceRequest(
    [Required, MaxLength(200)] string Title,
    string? Description,
    bool IsVisible,
    int Order
);

// ── Responses ─────────────────────────────────────────────────────────────────

public record CourseResourceDto(
    int Id,
    int CourseId,
    string Title,
    string? Description,
    string FileName,
    string FileUrl,
    string FileType,
    long FileSize,
    string FileSizeLabel,   // "2.4 MB", "512 KB"
    bool IsVisible,
    int Order,
    int DownloadCount,
    string UploadedByName,
    DateTime CreatedAt
);
