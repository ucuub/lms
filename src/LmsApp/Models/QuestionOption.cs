namespace LmsApp.Models;

public class QuestionOption
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; } = false;

    // Navigation
    public Question Question { get; set; } = null!;
}
