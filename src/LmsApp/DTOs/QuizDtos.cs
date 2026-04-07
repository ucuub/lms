using System.ComponentModel.DataAnnotations;
using LmsApp.Models;

namespace LmsApp.DTOs;

// ── Quiz CRUD ────────────────────────────────────────────────────────────────

public record QuizRequest(
    [Required, MaxLength(200)] string Title,
    string? Description,
    int TimeLimitMinutes = 30,
    int MaxAttempts = 1,
    int PassScore = 60,
    DateTime? DueDate = null,
    bool IsPublished = false
);

public record QuizResponse(
    int Id,
    int CourseId,
    string Title,
    string? Description,
    int TimeLimitMinutes,
    int MaxAttempts,
    int PassScore,
    DateTime? DueDate,
    bool IsPublished,
    int QuestionCount,
    int TotalPoints,
    int AttemptCount,
    DateTime CreatedAt
);

// ── Question CRUD ────────────────────────────────────────────────────────────

public record QuestionRequest(
    [Required] string Text,
    QuestionType Type = QuestionType.MultipleChoice,
    int Points = 10,
    int Order = 0,
    List<QuestionOptionRequest>? Options = null,
    string? CorrectAnswer = null // for essay: null; for TF: "True"/"False"
);

public record QuestionOptionRequest(
    [Required] string Text,
    bool IsCorrect = false
);

public record QuestionResponse(
    int Id,
    int QuizId,
    string Text,
    QuestionType Type,
    int Points,
    int Order,
    List<QuestionOptionResponse> Options
);

public record QuestionOptionResponse(
    int Id,
    string Text,
    bool IsCorrect
);

// ── Quiz Take ────────────────────────────────────────────────────────────────

public record QuizTakeResponse(
    int AttemptId,
    int QuizId,
    string QuizTitle,
    int TimeLimitMinutes,
    DateTime StartedAt,
    List<TakeQuestionDto> Questions
);

public record TakeQuestionDto(
    int Id,
    string Text,
    QuestionType Type,
    int Points,
    int Order,
    List<TakeOptionDto> Options
);

public record TakeOptionDto(
    int Id,
    string Text
);

public record SubmitQuizRequest(
    List<SubmitAnswerDto> Answers
);

public record SubmitAnswerDto(
    int QuestionId,
    int? SelectedOptionId,      // for MCQ / TF
    string? EssayAnswer         // for Essay
);

public record QuizResultResponse(
    int AttemptId,
    int QuizId,
    string QuizTitle,
    int Score,
    int MaxScore,
    double Percentage,
    bool Passed,
    int PassScore,
    DateTime StartedAt,
    DateTime? SubmittedAt,
    List<AnswerResultDto> Answers
);

public record AnswerResultDto(
    int QuestionId,
    string QuestionText,
    QuestionType Type,
    int Points,
    int? EarnedPoints,
    string? SelectedAnswer,
    string? CorrectAnswer,
    string? EssayAnswer,
    bool? IsCorrect,
    bool NeedsGrading
);

// ── Question Bank ────────────────────────────────────────────────────────────

public record QuestionBankRequest(
    [Required] string Text,
    QuestionType Type = QuestionType.MultipleChoice,
    string? Category = null,
    int Points = 1,
    List<QuestionOptionRequest>? Options = null
);

public record QuestionBankResponse(
    int Id,
    string OwnerId,
    string OwnerName,
    string? Category,
    string Text,
    QuestionType Type,
    int Points,
    List<QuestionOptionResponse> Options,
    DateTime CreatedAt
);

public record ImportFromBankRequest(
    List<int> QuestionBankIds
);
