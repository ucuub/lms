namespace LmsApp.Models;

public class QuizAttempt
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int AttemptNumber { get; set; } = 1;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public int Score { get; set; } = 0;
    public int MaxScore { get; set; } = 0;
    public bool IsPassed { get; set; } = false;
    public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;

    // Navigation
    public Quiz Quiz { get; set; } = null!;
    public ICollection<AttemptAnswer> Answers { get; set; } = [];
}

public enum AttemptStatus
{
    InProgress,
    Submitted,
    Graded
}
