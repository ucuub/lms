namespace LmsApp.DTOs;

public record NotificationDto(
    int Id,
    string Title,
    string Message,
    string? Link,
    string Type,
    bool IsRead,
    DateTime CreatedAt
);

public record NotificationListResponse(
    IEnumerable<NotificationDto> Items,
    int Total,
    int UnreadCount,
    int Page,
    int PageSize,
    int TotalPages
);
