namespace LmsApp.Models;

public class CourseModule
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? VideoUrl { get; set; }
    public string? VideoEmbedId { get; set; }
    public VideoProvider VideoProvider { get; set; } = VideoProvider.None;
    public ModuleContentType ContentType { get; set; } = ModuleContentType.Text;
    public int Order { get; set; }
    public bool IsPublished { get; set; } = true;
    public int DurationMinutes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Course Course { get; set; } = null!;
    public ICollection<ModuleAttachment> Attachments { get; set; } = [];
}

public enum ModuleContentType { Text, Video, Mixed }

public enum VideoProvider { None, YouTube, Vimeo }
