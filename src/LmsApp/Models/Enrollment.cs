namespace LmsApp.Models;

public class Enrollment
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string UserId { get; set; } = string.Empty; // Keycloak user ID
    public string UserName { get; set; } = string.Empty;
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public Course Course { get; set; } = null!;
}

public enum EnrollmentStatus
{
    Active,
    Completed,
    Dropped
}
