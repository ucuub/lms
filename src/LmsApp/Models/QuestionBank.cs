namespace LmsApp.Models;

public class QuestionBank
{
    public int Id { get; set; }
    public string OwnerId { get; set; } = string.Empty;  // teacher UserId
    public string OwnerName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; } = QuestionType.MultipleChoice;
    public int Points { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<QuestionBankOption> Options { get; set; } = [];
}

public class QuestionBankOption
{
    public int Id { get; set; }
    public int QuestionBankId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }

    public QuestionBank QuestionBank { get; set; } = null!;
}
