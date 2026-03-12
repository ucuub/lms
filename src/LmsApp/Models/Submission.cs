namespace LmsApp.Models;

public class Submission
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public string UserId { get; set; } = string.Empty; // Keycloak user ID
    public string UserName { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? FileUrl { get; set; }
    public int? Score { get; set; }
    public string? Feedback { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? GradedAt { get; set; }

    // Navigation
    public Assignment Assignment { get; set; } = null!;
}
