using System.ComponentModel.DataAnnotations;
using LmsApp.Models;

namespace LmsApp.DTOs;

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record ExamRequest(
    [Required, MaxLength(200)] string Title,
    string? Description,
    int TimeLimitMinutes = 60,
    int MaxAttempts = 1,
    int PassScore = 60,
    bool IsPublished = false
);

public record ExamQuestionRequest(
    [Required] string Text,
    QuestionType Type = QuestionType.MultipleChoice,
    int Points = 10,
    int Order = 0,
    List<ExamOptionRequest>? Options = null
);

public record ExamOptionRequest(string Text, bool IsCorrect);

public record SubmitExamRequest(List<ExamAnswerRequest> Answers);
public record ExamAnswerRequest(int QuestionId, int? SelectedOptionId, string? EssayAnswer);

public record GradeExamEssayRequest(int Points, string? Feedback);

// ── Response DTOs ─────────────────────────────────────────────────────────────

public record ExamSummaryResponse(
    int Id,
    string Title,
    string? Description,
    int TimeLimitMinutes,
    int MaxAttempts,
    int PassScore,
    bool IsPublished,
    string CreatedByName,
    int QuestionCount,
    int TotalPoints,
    int AttemptCount,
    DateTime CreatedAt
);

public record ExamDetailResponse(
    int Id,
    string Title,
    string? Description,
    int TimeLimitMinutes,
    int MaxAttempts,
    int PassScore,
    bool IsPublished,
    string CreatedBy,
    string CreatedByName,
    int QuestionCount,
    int TotalPoints,
    DateTime CreatedAt,
    IEnumerable<ExamQuestionResponse> Questions
);

public record ExamQuestionResponse(
    int Id,
    int ExamId,
    string Text,
    QuestionType Type,
    int Points,
    int Order,
    List<ExamOptionResponse> Options
);

public record ExamOptionResponse(int Id, string Text, bool IsCorrect);

// Response saat user mulai ujian — tanpa jawaban benar
public record ExamTakeResponse(
    int AttemptId,
    int ExamId,
    string Title,
    int TimeLimitMinutes,
    DateTime StartedAt,
    List<ExamTakeQuestionDto> Questions
);

public record ExamTakeQuestionDto(
    int Id,
    string Text,
    QuestionType Type,
    int Points,
    int Order,
    List<ExamTakeOptionDto> Options
);

public record ExamTakeOptionDto(int Id, string Text);

// Hasil ujian
public record ExamResultResponse(
    int AttemptId,
    int ExamId,
    string ExamTitle,
    int Score,
    int MaxScore,
    double Percentage,
    bool IsPassed,
    int PassScore,
    DateTime StartedAt,
    DateTime? SubmittedAt,
    List<ExamAnswerResultDto> Answers
);

public record ExamAnswerResultDto(
    int AnswerId,
    int QuestionId,
    string QuestionText,
    QuestionType QuestionType,
    int Points,
    int? EarnedPoints,
    string? SelectedAnswer,
    string? CorrectAnswer,
    string? EssayAnswer,
    bool? IsCorrect,
    bool NeedsGrading,
    string? Feedback
);

// Untuk bulk grading admin
public record ExamAttemptSummary(
    int AttemptId,
    string UserId,
    string UserName,
    DateTime StartedAt,
    DateTime? SubmittedAt,
    int Score,
    int MaxScore,
    double Percentage,
    bool IsPassed,
    bool HasUngradedEssay
);

public record ExamAttemptDetail(
    int AttemptId,
    string UserId,
    string UserName,
    DateTime StartedAt,
    DateTime? SubmittedAt,
    int Score,
    int MaxScore,
    double Percentage,
    bool IsPassed,
    List<ExamAnswerResultDto> Answers
);
