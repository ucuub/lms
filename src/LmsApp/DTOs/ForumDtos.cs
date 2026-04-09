using System.ComponentModel.DataAnnotations;

namespace LmsApp.DTOs;

// ── Requests ──────────────────────────────────────────────────────────────────

public record CreateThreadRequest(
    [Required, MaxLength(300)] string Title,
    [Required] string Body
);

/// <summary>
/// ParentId = null  → reply langsung ke root thread.
/// ParentId = &lt;id&gt; → nested reply ke reply lain (infinite depth).
/// </summary>
public record CreateReplyRequest(
    [Required] string Body,
    int? ParentId = null
);

public record UpdatePostRequest(
    [Required] string Body
);

// ── Thread list DTOs ──────────────────────────────────────────────────────────

public record ForumThreadDto(
    int Id,
    int CourseId,
    string UserId,
    string UserName,
    string Title,
    string Body,
    bool IsPinned,
    int ReplyCount,
    int LikeCount,
    bool IsLikedByMe,
    DateTime CreatedAt,
    DateTime? LastReplyAt
);

// ── Thread detail DTOs ────────────────────────────────────────────────────────

public record ForumThreadDetailDto(
    int Id,
    int CourseId,
    string UserId,
    string UserName,
    string Title,
    string Body,
    bool IsPinned,
    int LikeCount,
    bool IsLikedByMe,
    DateTime CreatedAt,
    DateTime? EditedAt,
    List<ForumReplyNestedDto> Replies
);

/// <summary>
/// Recursive DTO untuk nested replies.
/// Replies yang dihapus tetap ada di tree dengan body placeholder
/// agar struktur thread tidak rusak.
/// </summary>
public record ForumReplyNestedDto(
    int Id,
    int ThreadId,
    int? ParentId,
    string UserId,
    string UserName,
    string Body,
    bool IsDeleted,
    int LikeCount,
    bool IsLikedByMe,
    DateTime CreatedAt,
    DateTime? EditedAt,
    List<ForumReplyNestedDto> Replies
);

// ── Like ──────────────────────────────────────────────────────────────────────

public record LikeResultDto(int PostId, int LikeCount, bool IsLiked);
