namespace LmsApp.Models;

public class ModuleAttachment
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public CourseModule Module { get; set; } = null!;
}
