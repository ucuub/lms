namespace LmsApp.Models;

public class ActivityLog
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;      // "view_module", "submit_assignment", etc.
    public string EntityType { get; set; } = string.Empty;  // "Module", "Assignment", "Quiz", "Resource"
    public int? EntityId { get; set; }
    public string? EntityTitle { get; set; }
    public int? CourseId { get; set; }
    public string? Metadata { get; set; }                   // JSON string for extra data
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
