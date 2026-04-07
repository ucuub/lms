using System.ComponentModel.DataAnnotations;

namespace LmsApp.DTOs;

public record AssignmentRequest(
    [Required, MaxLength(200)] string Title,
    [Required] string Description,
    DateTime? DueDate,
    int MaxScore = 100
);

public record AssignmentResponse(
    int Id,
    int CourseId,
    string Title,
    string Description,
    DateTime? DueDate,
    int MaxScore,
    int SubmissionCount,
    int GradedCount,
    DateTime CreatedAt
);

public record SubmissionRequest(
    string? TextContent,
    string? FileUrl
);

public record SubmissionResponse(
    int Id,
    int AssignmentId,
    string AssignmentTitle,
    string UserId,
    string UserName,
    string? TextContent,
    string? FileUrl,
    string? FileName,
    int? Score,
    string? Feedback,
    string Status,
    DateTime SubmittedAt,
    DateTime? GradedAt
);

public record GradeSubmissionRequest(
    [Range(0, 10000)] int Score,
    string? Feedback
);
