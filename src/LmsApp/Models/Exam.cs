namespace LmsApp.Models;

public class Exam
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TimeLimitMinutes { get; set; } = 60;
    public int MaxAttempts { get; set; } = 1;
    public int PassScore { get; set; } = 60; // persentase
    public bool IsPublished { get; set; } = false;
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<ExamQuestion> Questions { get; set; } = [];
    public ICollection<ExamAttempt> Attempts { get; set; } = [];
}

public class ExamQuestion
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; } = QuestionType.MultipleChoice;
    public int Points { get; set; } = 10;
    public int Order { get; set; }

    // Navigation
    public Exam Exam { get; set; } = null!;
    public ICollection<ExamQuestionOption> Options { get; set; } = [];
}

public class ExamQuestionOption
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }

    // Navigation
    public ExamQuestion Question { get; set; } = null!;
}

public class ExamAttempt
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public bool IsPassed { get; set; }

    // Navigation
    public Exam Exam { get; set; } = null!;
    public ICollection<ExamAnswer> Answers { get; set; } = [];
}

public class ExamAnswer
{
    public int Id { get; set; }
    public int AttemptId { get; set; }
    public int QuestionId { get; set; }
    public int? SelectedOptionId { get; set; }
    public string? EssayAnswer { get; set; }
    public bool? IsCorrect { get; set; }
    public int? EarnedPoints { get; set; }
    public string? Feedback { get; set; }

    // Navigation
    public ExamAttempt Attempt { get; set; } = null!;
    public ExamQuestion Question { get; set; } = null!;
}
