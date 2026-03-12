namespace LmsApp.Models;

public class AttemptAnswer
{
    public int Id { get; set; }
    public int AttemptId { get; set; }
    public int QuestionId { get; set; }
    public int? SelectedOptionId { get; set; }
    public string? EssayAnswer { get; set; }
    public int? ManualScore { get; set; }     // for essay grading
    public bool IsCorrect { get; set; } = false;

    // Navigation
    public QuizAttempt Attempt { get; set; } = null!;
    public Question Question { get; set; } = null!;
}
