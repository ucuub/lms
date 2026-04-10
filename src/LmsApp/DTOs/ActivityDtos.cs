namespace LmsApp.DTOs;

public record ActivityLogDto(
    int Id,
    string UserId,
    string UserName,
    string Action,
    string EntityType,
    int? EntityId,
    string? EntityTitle,
    int? CourseId,
    string? Metadata,
    DateTime Timestamp
);
