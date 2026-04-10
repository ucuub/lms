namespace LmsApp.DTOs;

public record SearchResultDto(
    IEnumerable<CourseSearchItem> Courses,
    IEnumerable<ModuleSearchItem> Modules,
    IEnumerable<ForumSearchItem> ForumPosts
);

public record CourseSearchItem(int Id, string Title, string? Description, string? ThumbnailUrl, string Category, string Level, int EnrollmentCount);
public record ModuleSearchItem(int Id, int CourseId, string CourseTitle, string Title, string ContentType);
public record ForumSearchItem(int Id, int CourseId, string CourseTitle, string? Title, string Body, string UserName, DateTime CreatedAt);
