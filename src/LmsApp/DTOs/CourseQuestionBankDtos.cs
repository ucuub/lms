using LmsApp.Models;

namespace LmsApp.DTOs;

// ── Request DTOs ─────────────────────────────────────────────────────────────

/// Dipakai untuk Create maupun Update — struktur identik sehingga satu class cukup.
public class SaveCourseQuestionRequest
{
    public int? ModuleId { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; } = QuestionType.MultipleChoice;
    public int Points { get; set; } = 10;
    public string? Explanation { get; set; }
    public List<CourseOptionRequest> Options { get; set; } = [];
}

public class CourseOptionRequest
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

// ── Response DTOs ─────────────────────────────────────────────────────────────

public record CourseQuestionBankResponse(
    int Id,
    int CourseId,
    int? ModuleId,
    string? ModuleTitle,
    string Text,
    string Type,
    int Points,
    string? Explanation,
    string CreatedBy,
    string CreatedByName,
    DateTime CreatedAt,
    List<CourseOptionResponse> Options
);

public record CourseOptionResponse(
    int Id,
    string Text,
    bool IsCorrect
);
