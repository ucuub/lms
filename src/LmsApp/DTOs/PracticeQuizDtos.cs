namespace LmsApp.DTOs;

// ── List / Detail ─────────────────────────────────────────────────────────────

public record PracticeQuizDto(
    int Id,
    string Title,
    string? Description,
    int QuestionCount,
    bool ShuffleQuestions,
    bool ShuffleOptions,
    int? TimeLimitMinutes,
    bool IsActive,
    string CreatedByName,
    DateTime CreatedAt,
    int MyAttemptCount  // berapa kali user ini sudah mencoba
);

public record CreatePracticeQuizRequest(
    string Title,
    string? Description,
    int QuestionCount,
    bool ShuffleQuestions,
    bool ShuffleOptions,
    int? TimeLimitMinutes
);

// ── Start Attempt ─────────────────────────────────────────────────────────────

public record StartPracticeResponse(
    int AttemptId,
    int PracticeQuizId,
    string QuizTitle,
    int? TimeLimitMinutes,
    List<PracticeQuestionDto> Questions
);

public record PracticeQuestionDto(
    int Id,       // BankQuestion.Id — dipakai saat submit
    string Text,
    string Type,  // "MultipleChoice" | "TrueFalse" | "Essay"
    int Points,
    int DisplayOrder,
    List<PracticeOptionDto>? Options  // null untuk Essay
);

public record PracticeOptionDto(
    int Id,
    string Text
    // IsCorrect TIDAK dikirim ke frontend
);

// ── Submit ────────────────────────────────────────────────────────────────────

public record SubmitPracticeRequest(List<PracticeAnswerInput> Answers);

public record PracticeAnswerInput(int QuestionId, int? SelectedOptionId);

// ── Result ────────────────────────────────────────────────────────────────────

public record PracticeResultDto(
    int AttemptId,
    int PracticeQuizId,
    string QuizTitle,
    double Score,
    int TotalQuestions,
    int CorrectAnswers,
    DateTime StartedAt,
    DateTime SubmittedAt,
    List<PracticeResultDetailDto> Details
);

public record PracticeResultDetailDto(
    int QuestionId,
    string QuestionText,
    string Type,
    bool IsCorrect,
    int? SelectedOptionId,
    string? SelectedOptionText,
    int? CorrectOptionId,
    string? CorrectOptionText
);

// ── My Attempts ───────────────────────────────────────────────────────────────

public record PracticeAttemptSummaryDto(
    int Id,
    int PracticeQuizId,
    string QuizTitle,
    DateTime StartedAt,
    DateTime? SubmittedAt,
    double? Score,
    int TotalQuestions,
    int? CorrectAnswers,
    bool IsCompleted
);
