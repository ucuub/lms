namespace LmsApp.Models;

/// <summary>
/// Aturan yang harus dipenuhi student agar course dianggap selesai.
/// Satu course = satu rule (one-to-one). Jika belum ada rule,
/// default: semua modul published harus selesai (100%).
/// </summary>
public class CourseCompletionRule
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    /// <summary>
    /// Persentase minimum modul yang harus diselesaikan (0–100).
    /// Default 100 = semua modul harus selesai.
    /// </summary>
    public int RequiredModulePercent { get; set; } = 100;

    /// <summary>
    /// Jika true, student juga harus submit semua assignment aktif.
    /// </summary>
    public bool RequireAllAssignments { get; set; } = false;

    /// <summary>
    /// Jika true, student harus lulus (IsPassed = true) semua quiz published.
    /// </summary>
    public bool RequireAllQuizzesPassed { get; set; } = false;

    /// <summary>
    /// Jika true, student harus lulus ujian tertentu (RequiredExamId) untuk mendapat sertifikat.
    /// </summary>
    public bool RequireExamPassed { get; set; } = false;

    /// <summary>
    /// ID QuestionSet yang harus dilulus. Null jika RequireExamPassed = false.
    /// </summary>
    public int? RequiredExamId { get; set; }

    public QuestionSet? RequiredExam { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
