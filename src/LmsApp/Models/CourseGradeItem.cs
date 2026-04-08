namespace LmsApp.Models;

/// <summary>
/// Satu "kolom" di gradebook course (mirip Moodle Grade Item).
/// Bisa berasal dari Assignment, Quiz, atau diisi manual.
/// Teacher bisa set weight untuk weighted average.
/// </summary>
public class CourseGradeItem
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    // Link ke sumber — tepat satu harus di-set (kecuali Manual)
    public int? AssignmentId { get; set; }
    public Assignment? Assignment { get; set; }

    public int? QuizId { get; set; }
    public Quiz? Quiz { get; set; }

    public string Name { get; set; } = string.Empty;   // "Tugas 1: Membuat HTML", "Quiz Akhir", dll
    public GradeItemType Type { get; set; }
    public int MaxScore { get; set; } = 100;

    /// <summary>
    /// Bobot dalam weighted average.
    /// Contoh: Assignment=2.0, Quiz=1.0 → assignment bobotnya 2x quiz.
    /// Default 1.0 = bobot sama semua.
    /// </summary>
    public double Weight { get; set; } = 1.0;

    public int Order { get; set; } = 0;       // urutan di gradebook
    public bool IsVisible { get; set; } = true; // bisa disembunyikan dari student

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Manual grades dan overrides
    public ICollection<CourseGradeEntry> Entries { get; set; } = [];
}

public enum GradeItemType
{
    Assignment = 0,
    Quiz = 1,
    Manual = 2   // nilai yang diinput langsung oleh teacher
}
