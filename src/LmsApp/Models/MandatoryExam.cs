namespace LmsApp.Models;

public class MandatoryExam
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? TimeLimitMinutes { get; set; }
    public int MaxAttempts { get; set; } = 1;
    public int PassScore { get; set; } = 60; // persen
    public bool IsActive { get; set; } = true;
    /// <summary>UUID untuk public link (satu link per exam, tanpa userId).
    /// Null = belum digenerate. DWI Mobile append userId saat membuka.</summary>
    public string? PublicAccessCode { get; set; }

    /// <summary>Webhook URL DWI Mobile — dipanggil otomatis saat user submit ujian.</summary>
    public string? WebhookUrl { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MandatoryExamQuestion> Questions { get; set; } = [];
    public ICollection<MandatoryExamAssignment> Assignments { get; set; } = [];
}

public class MandatoryExamQuestion
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; } = QuestionType.MultipleChoice;
    public int Points { get; set; } = 10;
    public int Order { get; set; }

    public MandatoryExam Exam { get; set; } = null!;
    public ICollection<MandatoryExamOption> Options { get; set; } = [];
}

public class MandatoryExamOption
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }

    public MandatoryExamQuestion Question { get; set; } = null!;
}

public enum MandatoryExamAssignmentStatus { NotYet, InProgress, Done }

public class MandatoryExamAssignment
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public MandatoryExamAssignmentStatus Status { get; set; } = MandatoryExamAssignmentStatus.NotYet;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public MandatoryExam Exam { get; set; } = null!;
    public ICollection<MandatoryExamAttempt> Attempts { get; set; } = [];
}

public class MandatoryExamAttempt
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public int ExamId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public int? Score { get; set; }
    public int? MaxScore { get; set; }
    public bool? IsPassed { get; set; }

    public MandatoryExamAssignment Assignment { get; set; } = null!;
    public ICollection<MandatoryExamAnswer> Answers { get; set; } = [];
}

public class MandatoryExamAnswer
{
    public int Id { get; set; }
    public int AttemptId { get; set; }
    public int QuestionId { get; set; }
    public int? SelectedOptionId { get; set; }
    public string? EssayAnswer { get; set; }
    public bool? IsCorrect { get; set; }
    public int? EarnedPoints { get; set; }
    public string? Feedback { get; set; }

    public MandatoryExamAttempt Attempt { get; set; } = null!;
    public MandatoryExamQuestion Question { get; set; } = null!;
}

/// <summary>
/// Tracks every deep-link token that was generated.
/// Allows revocation and audit trail without storing the full token string.
/// </summary>
public class MandatoryExamSession
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string TokenJti { get; set; } = string.Empty; // JWT jti claim — unique per token
    public string GeneratedBy { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }   // null = belum pernah dipakai
    public bool IsRevoked { get; set; } = false;
    public bool IsLinkToken { get; set; } = false;   // true = public link token, false = personal token
    public int? ParentSessionId { get; set; }          // for personal tokens: ref to the link session
    public int MaxUsageCount { get; set; } = 5;
    public int CurrentUsageCount { get; set; } = 0;

    public MandatoryExam Exam { get; set; } = null!;
}
