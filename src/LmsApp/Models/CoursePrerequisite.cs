namespace LmsApp.Models;

/// <summary>
/// Mendefinisikan prerequisite antar course.
/// CourseId = kursus yang membutuhkan prerequisite.
/// PrerequisiteCourseId = kursus yang harus sudah diselesaikan terlebih dahulu.
/// </summary>
public class CoursePrerequisite
{
    public int Id { get; set; }

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public int PrerequisiteCourseId { get; set; }
    public Course PrerequisiteCourse { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
