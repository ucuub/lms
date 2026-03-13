namespace LmsApp.Models;

public class CourseModule
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }       // HTML dari rich text editor
    public string? VideoUrl { get; set; }       // YouTube / Vimeo URL
    public string? VideoEmbedId { get; set; }  // parsed embed ID
    public VideoProvider? VideoProvider { get; set; }
    public ModuleContentType ContentType { get; set; } = ModuleContentType.Text;
    public int Order { get; set; }
    public bool IsPublished { get; set; } = true;
    public int? DurationMinutes { get; set; }  // estimasi durasi
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Course Course { get; set; } = null!;
    public ICollection<ModuleAttachment> Attachments { get; set; } = [];
}

public enum ModuleContentType
{
    Text,
    Video,
    Mixed   // text + video
}

public enum VideoProvider
{
    YouTube,
    Vimeo
}
