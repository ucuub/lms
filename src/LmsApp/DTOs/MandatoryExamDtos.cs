namespace LmsApp.DTOs;

// ── Management Requests ───────────────────────────────────────────────────────

public record CreateMandatoryExamRequest(
    string Title,
    string? Description,
    int? TimeLimitMinutes,
    int MaxAttempts,
    int PassScore
);

public record AddMandatoryQuestionRequest(
    string Text,
    string Type,
    int Points,
    List<MandatoryOptionRequest> Options
);

public record MandatoryOptionRequest(string Text, bool IsCorrect);

public record AssignMandatoryExamRequest(string UserId, string UserName);

public record GenerateMandatoryLinkRequest(string UserId, int ExpiryMinutes = 60);

// ── Session Requests ──────────────────────────────────────────────────────────

public record SubmitMandatoryExamRequest(List<MandatoryAnswerRequest> Answers);

public record MandatoryAnswerRequest(
    int QuestionId,
    int? SelectedOptionId,
    string? EssayAnswer
);

// ── Responses ─────────────────────────────────────────────────────────────────

public record MandatoryExamSummaryResponse(
    int Id,
    string Title,
    string? Description,
    int? TimeLimitMinutes,
    int MaxAttempts,
    int PassScore,
    bool IsActive,
    int QuestionCount,
    int AssignmentCount,
    DateTime CreatedAt
);

public record MandatoryExamDetailResponse(
    int Id,
    string Title,
    string? Description,
    int? TimeLimitMinutes,
    int MaxAttempts,
    int PassScore,
    bool IsActive,
    string CreatedByName,
    DateTime CreatedAt,
    List<MandatoryQuestionResponse> Questions,
    List<MandatoryAssignmentResponse> Assignments
);

public record MandatoryQuestionResponse(
    int Id,
    int ExamId,
    string Text,
    string Type,
    int Points,
    int Order,
    List<MandatoryOptionResponse> Options
);

public record MandatoryOptionResponse(int Id, string Text, bool IsCorrect);

public record MandatoryAssignmentResponse(
    int Id,
    string UserId,
    string UserName,
    string Status,
    DateTime AssignedAt,
    DateTime? CompletedAt,
    int AttemptCount
);

public record GenerateMandatoryLinkResponse(string Token, string DeepLink, DateTime ExpiresAt, int SessionId);

public record MandatoryExamSessionResponse(
    int Id,
    int ExamId,
    string UserId,
    string GeneratedBy,
    DateTime GeneratedAt,
    DateTime ExpiresAt,
    DateTime? UsedAt,
    bool IsRevoked,
    bool IsExpired
);

// ── Deep Link Session Responses ───────────────────────────────────────────────

public record ValidateMandatoryTokenResponse(
    int AttemptId,
    int ExamId,
    string ExamTitle,
    string? ExamDescription,
    int? TimeLimitMinutes,
    bool IsResume,
    DateTime StartedAt,
    List<MandatoryQuestionResponse> Questions
);

public record MandatoryExamResultResponse(
    int AttemptId,
    int ExamId,
    string ExamTitle,
    int Score,
    int MaxScore,
    int Percentage,
    bool IsPassed,
    int PassScore,
    DateTime StartedAt,
    DateTime SubmittedAt,
    List<MandatoryAnswerResultResponse> Answers
);

public record MandatoryAnswerResultResponse(
    int QuestionId,
    string QuestionText,
    string QuestionType,
    int Points,
    int? SelectedOptionId,
    string? SelectedOptionText,
    string? EssayAnswer,
    bool? IsCorrect,
    int? EarnedPoints,
    string? Feedback,
    string? CorrectAnswer
);
