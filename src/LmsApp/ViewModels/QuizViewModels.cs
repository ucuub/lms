using System.ComponentModel.DataAnnotations;

namespace LmsApp.ViewModels;

public class QuizCreateViewModel
{
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Judul quiz wajib diisi")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(5, 180, ErrorMessage = "Waktu antara 5-180 menit")]
    public int TimeLimitMinutes { get; set; } = 30;

    [Range(1, 10, ErrorMessage = "Maksimal percobaan 1-10")]
    public int MaxAttempts { get; set; } = 1;

    [Range(0, 100, ErrorMessage = "Nilai lulus 0-100")]
    public int PassScore { get; set; } = 60;

    public DateTime? DueDate { get; set; }
    public bool IsPublished { get; set; } = false;
}
