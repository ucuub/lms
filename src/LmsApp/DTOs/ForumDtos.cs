using System.ComponentModel.DataAnnotations;

namespace LmsApp.DTOs;

public record CreateThreadRequest(
    [Required, MaxLength(300)] string Title,
    [Required] string Body
);

public record CreateReplyRequest(
    [Required] string Body
);

public record ForumThreadDto(
    int Id,
    int CourseId,
    string UserId,
    string UserName,
    string Title,
    string Body,
    bool IsPinned,
    int ReplyCount,
    DateTime CreatedAt
);

public record ForumThreadDetailDto(
    int Id,
    int CourseId,
    string UserId,
    string UserName,
    string Title,
    string Body,
    bool IsPinned,
    DateTime CreatedAt,
    List<ForumReplyDto> Replies
);

public record ForumReplyDto(
    int Id,
    int ThreadId,
    string UserId,
    string UserName,
    string Body,
    DateTime CreatedAt
);
