using System.ComponentModel.DataAnnotations;
using LmsApp.Models;

namespace LmsApp.DTOs;

public record ModuleRequest(
    [Required, MaxLength(200)] string Title,
    string? Content,
    string? VideoUrl,
    int Order = 0,
    bool IsPublished = false,
    int DurationMinutes = 0,
    int? SectionId = null          // NEW: optional, null = unsectioned (backward compatible)
);

public record ModuleResponse(
    int Id,
    int CourseId,
    int? SectionId,                // NEW: section yang menampung modul ini
    string Title,
    string? Content,
    string? VideoUrl,
    string? VideoEmbedId,
    string VideoProvider,
    string ContentType,
    int Order,
    bool IsPublished,
    int DurationMinutes,
    List<AttachmentDto> Attachments
);

public record AttachmentDto(
    int Id,
    string FileName,
    string FileUrl,
    long FileSize,
    string FileType
);

public record ReorderRequest(List<ReorderItem> Items);
public record ReorderItem(int Id, int Order);
