namespace LmsApp.Models;

public class PracticeAttemptAnswer
{
    public int Id { get; set; }
    public int AttemptId { get; set; }
    public int BankQuestionId { get; set; }   // FK → QuestionBank.Id
    public int? SelectedOptionId { get; set; } // FK → QuestionBankOption.Id (null = unanswered)
    public int DisplayOrder { get; set; }

    // Navigation
    public PracticeAttempt Attempt { get; set; } = null!;
    public QuestionBank BankQuestion { get; set; } = null!;
    public QuestionBankOption? SelectedOption { get; set; }
}
