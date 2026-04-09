namespace LmsApp.Models;

public class ForumPost
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int? ParentId { get; set; }        // null = thread, set = reply

    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;   // only for parent posts
    public string Body { get; set; } = string.Empty;
    public bool IsPinned { get; set; } = false;
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }

    /// <summary>
    /// Denormalized FK to the root thread. Null for root threads, set for all
    /// replies at any depth — allows loading the entire thread in one query.
    /// </summary>
    public int? RootThreadId { get; set; }

    // Navigation
    public Course Course { get; set; } = null!;
    public ForumPost? Parent { get; set; }
    public ICollection<ForumPost> Replies { get; set; } = [];
    public ICollection<ForumLike> Likes { get; set; } = [];
}
