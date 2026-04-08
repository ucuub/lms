namespace LmsApp.Models;

/// <summary>
/// Nilai manual atau override dari teacher untuk satu grade item + satu student.
/// Untuk Assignment/Quiz, score aslinya dari Submission/QuizAttempt.
/// Entry ini digunakan untuk: tipe Manual, atau override nilai.
/// </summary>
public class CourseGradeEntry
{
    public int Id { get; set; }
    public int GradeItemId { get; set; }
    public CourseGradeItem GradeItem { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    public double Score { get; set; }
    public string? Comment { get; set; }

    public DateTime GradedAt { get; set; } = DateTime.UtcNow;
    public string GradedByUserId { get; set; } = string.Empty;
}
