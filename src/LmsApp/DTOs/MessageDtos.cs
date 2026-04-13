namespace LmsApp.DTOs;

public record ConversationDto(
    int Id,
    string OtherUserId,
    string OtherUserName,
    string? LastMessage,
    DateTime? LastMessageAt,
    int UnreadCount,
    DateTime CreatedAt
);

public record MessageDto(
    int Id,
    int ConversationId,
    string SenderId,
    string SenderName,
    string Content,
    bool IsRead,
    DateTime CreatedAt
);

public record SendMessageRequest(string RecipientId, string Content, string? RecipientName = null);
