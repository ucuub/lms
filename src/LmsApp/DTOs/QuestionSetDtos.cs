using System.ComponentModel.DataAnnotations;
using LmsApp.Models;

namespace LmsApp.DTOs;

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record QuestionSetRequest(
    [Required, MaxLength(200)] string Title,
    string? Description,
    int? TimeLimitMinutes = null,
    int MaxAttempts = 1,
    int PassScore = 60,
    bool IsPublished = false
);

public record QuestionSetQuestionRequest(
    [Required] string Text,
    QuestionType Type = QuestionType.MultipleChoice,
    int Points = 10,
    int Order = 0,
    List<QuestionSetOptionRequest>? Options = null
);

public record QuestionSetOptionRequest(string Text, bool IsCorrect);

public record ImportQuestionsFromBankRequest(List<int> QuestionBankIds);

public record SubmitQuestionSetRequest(List<QuestionSetAnswerRequest> Answers);
public record QuestionSetAnswerRequest(int QuestionId, int? SelectedOptionId, string? EssayAnswer);

public record GradeQuestionSetEssayRequest(int Points, string? Feedback);

// ── Response DTOs ─────────────────────────────────────────────────────────────

public record QuestionSetSummaryResponse(
    int Id,
    string Title,
    string? Description,
    int? TimeLimitMinutes,
    int MaxAttempts,
    int PassScore,
    bool IsPublished,
    string CreatedBy,
    string CreatedByName,
    int QuestionCount,
    int TotalPoints,
    int AttemptCount,
    int? MyAttemptCount,
    DateTime CreatedAt
);

public record QuestionSetDetailResponse(
    int Id,
    string Title,
    string? Description,
    int? TimeLimitMinutes,
    int MaxAttempts,
    int PassScore,
    bool IsPublished,
    string CreatedBy,
    string CreatedByName,
    int QuestionCount,
    int TotalPoints,
    DateTime CreatedAt,
    IEnumerable<QuestionSetQuestionResponse> Questions
);

public record QuestionSetQuestionResponse(
    int Id,
    int QuestionSetId,
    int? BankQuestionId,
    string Text,
    QuestionType Type,
    int Points,
    int Order,
    List<QuestionSetOptionResponse> Options
);

public record QuestionSetOptionResponse(int Id, string Text, bool IsCorrect);

// Response saat user mulai mengerjakan — tanpa jawaban benar
public record QuestionSetTakeResponse(
    int AttemptId,
    int QuestionSetId,
    string Title,
    int? TimeLimitMinutes,
    DateTime StartedAt,
    List<QuestionSetTakeQuestionDto> Questions
);

public record QuestionSetTakeQuestionDto(
    int Id,
    string Text,
    QuestionType Type,
    int Points,
    int Order,
    List<QuestionSetTakeOptionDto> Options
);

public record QuestionSetTakeOptionDto(int Id, string Text);

// Hasil pengerjaan
public record QuestionSetResultResponse(
    int AttemptId,
    int QuestionSetId,
    string QuestionSetTitle,
    int Score,
    int MaxScore,
    double Percentage,
    bool IsPassed,
    int PassScore,
    DateTime StartedAt,
    DateTime? SubmittedAt,
    List<QuestionSetAnswerResultDto> Answers
);

public record QuestionSetAnswerResultDto(
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

// Untuk grading admin/teacher
public record QuestionSetAttemptSummary(
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

public record QuestionSetAttemptDetail(
    int AttemptId,
    string UserId,
    string UserName,
    DateTime StartedAt,
    DateTime? SubmittedAt,
    int Score,
    int MaxScore,
    double Percentage,
    bool IsPassed,
    List<QuestionSetAnswerResultDto> Answers
);
