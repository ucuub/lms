namespace LmsApp.Models;

public class CourseQuestionBank
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int? ModuleId { get; set; }   // nullable — soal bisa milik modul tertentu atau kursus umum
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; } = QuestionType.MultipleChoice;
    public int Points { get; set; } = 10;
    public string? Explanation { get; set; }  // penjelasan jawaban benar
    public string CreatedBy { get; set; } = string.Empty;  // teacher UserId
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Course Course { get; set; } = null!;
    public CourseModule? Module { get; set; }
    public ICollection<CourseQuestionBankOption> Options { get; set; } = [];
}

public class CourseQuestionBankOption
{
    public int Id { get; set; }
    public int CourseQuestionBankId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }

    public CourseQuestionBank Question { get; set; } = null!;
}
