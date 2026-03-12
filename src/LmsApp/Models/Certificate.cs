namespace LmsApp.Models;

public class Certificate
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string CertificateNumber { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Course Course { get; set; } = null!;
}
