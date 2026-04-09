namespace LmsApp.Models;

/// <summary>
/// File resource yang di-attach ke course (bukan ke modul tertentu).
/// Contoh: syllabus, slide perkuliahan, bahan bacaan tambahan, video intro.
/// </summary>
public class CourseResource
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string FileName { get; set; } = string.Empty;  // nama file asli
    public string FileUrl { get; set; } = string.Empty;   // path di server
    public string FileType { get; set; } = string.Empty;  // "pdf" | "video" | "doc" | "image" | "other"
    public long FileSize { get; set; }

    public bool IsVisible { get; set; } = true;
    public int Order { get; set; }
    public int DownloadCount { get; set; }

    public string UploadedBy { get; set; } = string.Empty;    // UserId
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
