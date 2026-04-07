namespace LmsApp.Models;

public class AttemptAnswer
{
    public int Id { get; set; }
    public int AttemptId { get; set; }
    public int QuestionId { get; set; }
    public int? SelectedOptionId { get; set; }
    public string? EssayAnswer { get; set; }
    public int? EarnedPoints { get; set; }
    public bool? IsCorrect { get; set; }     // null = not graded yet (essay)
    public string? Feedback { get; set; }

    // Navigation
    public QuizAttempt Attempt { get; set; } = null!;
    public Question Question { get; set; } = null!;
}
