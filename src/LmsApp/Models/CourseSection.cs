namespace LmsApp.Models;

/// <summary>
/// Pengelompokan modul di dalam course, seperti "Week 1", "Topic A", dsb.
/// Satu Course punya banyak Section. Satu Section punya banyak Module.
/// SectionId di CourseModule bersifat nullable (backward compatible).
/// </summary>
public class CourseSection
{
    public int Id { get; set; }

    // FK ke Course — section ini milik course mana
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public string Title { get; set; } = string.Empty;   // "Week 1: Introduction", "Bab 2", dsb
    public string? Description { get; set; }             // deskripsi singkat optional

    public int Order { get; set; }                       // urutan tampil (0, 1, 2, ...)
    public bool IsVisible { get; set; } = true;          // false = student tidak lihat

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation — modul yang tergabung dalam section ini
    public ICollection<CourseModule> Modules { get; set; } = [];
}
