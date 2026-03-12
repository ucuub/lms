namespace LmsApp.Models;

public class Question
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; } = QuestionType.MultipleChoice;
    public int Points { get; set; } = 10;
    public int Order { get; set; }

    // Navigation
    public Quiz Quiz { get; set; } = null!;
    public ICollection<QuestionOption> Options { get; set; } = [];
}

public enum QuestionType
{
    MultipleChoice,
    TrueFalse,
    Essay
}
