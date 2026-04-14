using System.ComponentModel.DataAnnotations;
using LmsApp.Models;

namespace LmsApp.DTOs;

public record CourseRequest(
    [Required, MaxLength(200)] string Title,
    [Required] string Description,
    string? Category,
    string Level = "Semua",
    bool IsPublished = false
);

public record CourseResponse(
    int Id,
    string Title,
    string Description,
    string? ThumbnailUrl,
    string InstructorId,
    string InstructorName,
    string? Category,
    string Level,
    bool IsPublished,
    int EnrollmentCount,
    double AverageRating,
    int ReviewCount,
    int ModuleCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CourseDetailResponse(
    int Id,
    string Title,
    string Description,
    string? ThumbnailUrl,
    string InstructorId,
    string InstructorName,
    string? Category,
    string Level,
    bool IsPublished,
    int EnrollmentCount,
    double AverageRating,
    int ReviewCount,
    bool IsEnrolled,
    EnrollmentStatus? EnrollmentStatus,
    // Sections berisi modul yang sudah punya section (terurut per section)
    IEnumerable<SectionDetailDto> Sections,
    // UnsectionedModules = modul yang belum masuk section manapun (data lama / backward compat)
    IEnumerable<ModuleSummaryDto> UnsectionedModules,
    IEnumerable<AssignmentSummaryDto> Assignments,
    IEnumerable<QuizSummaryDto> Quizzes,
    IEnumerable<AnnouncementDto> Announcements,
    IEnumerable<ReviewDto> Reviews,
    DateTime CreatedAt
);

public record ModuleSummaryDto(
    int Id,
    string Title,
    int Order,
    bool IsPublished,
    int DurationMinutes,
    string ContentType,
    int? SectionId = null,       // nullable default agar backward compat saat construct lama
    string? VideoEmbedId = null, // untuk preview thumbnail di course detail
    string? VideoProvider = null
);

public record AssignmentSummaryDto(
    int Id,
    string Title,
    DateTime? DueDate,
    int MaxScore
);

public record QuizSummaryDto(
    int Id,
    string Title,
    int TimeLimitMinutes,
    int PassScore,
    DateTime? DueDate,
    bool IsPublished,
    int QuestionCount
);

public record AnnouncementDto(
    int Id,
    string Title,
    string Content,
    bool IsPinned,
    DateTime CreatedAt
);

public record ReviewDto(
    int Id,
    string UserId,
    string UserName,
    int Rating,
    string? Comment,
    DateTime CreatedAt
);

public record CourseFilterRequest(
    string? Search,
    string? Category,
    string? Level,
    string Sort = "newest",
    int Page = 1,
    int PageSize = 12
);

public record PagedResponse<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
