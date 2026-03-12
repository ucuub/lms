namespace LmsApp.Models;

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string InstructorId { get; set; } = string.Empty; // Keycloak user ID
    public string InstructorName { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<Assignment> Assignments { get; set; } = [];
    public ICollection<CourseModule> Modules { get; set; } = [];
}
