namespace LmsApp.Models;

public class Submission
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? TextContent { get; set; }
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public int? Score { get; set; }
    public string? Feedback { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? GradedAt { get; set; }

    // Navigation
    public Assignment Assignment { get; set; } = null!;
}

public enum SubmissionStatus { Submitted, Graded }
