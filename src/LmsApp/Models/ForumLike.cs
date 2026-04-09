namespace LmsApp.Models;

/// <summary>
/// Like / upvote pada sebuah forum post (thread maupun reply).
/// Unique per (PostId, UserId) — satu user hanya bisa like sekali.
/// </summary>
public class ForumLike
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ForumPost Post { get; set; } = null!;
}
