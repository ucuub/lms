using System.ComponentModel.DataAnnotations;
using LmsApp.Models;

namespace LmsApp.ViewModels;

public class QuestionCreateViewModel
{
    public int QuizId { get; set; }

    [Required(ErrorMessage = "Teks soal wajib diisi")]
    public string Text { get; set; } = string.Empty;

    public QuestionType Type { get; set; } = QuestionType.MultipleChoice;

    [Range(1, 100, ErrorMessage = "Poin antara 1-100")]
    public int Points { get; set; } = 10;

    // For MCQ
    public List<OptionInput> Options { get; set; } =
    [
        new(), new(), new(), new()
    ];

    // For TrueFalse
    public bool? CorrectTrueFalse { get; set; } = true;
}

public class OptionInput
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; } = false;
}
