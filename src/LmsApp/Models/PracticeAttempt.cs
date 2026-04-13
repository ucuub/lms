namespace LmsApp.Models;

public class PracticeAttempt
{
    public int Id { get; set; }
    public int PracticeQuizId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public double? Score { get; set; }
    public int TotalQuestions { get; set; }
    public int? CorrectAnswers { get; set; }

    // Navigation
    public PracticeQuiz PracticeQuiz { get; set; } = null!;
    public ICollection<PracticeAttemptAnswer> Answers { get; set; } = [];
}
