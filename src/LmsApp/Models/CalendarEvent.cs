namespace LmsApp.Models;

public class CalendarEvent
{
    public int Id { get; set; }
    public int? CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public CalendarEventType Type { get; set; } = CalendarEventType.Event;
    public string? CreatedByUserId { get; set; }

    // Navigation
    public Course? Course { get; set; }
}

public enum CalendarEventType
{
    Assignment,
    Quiz,
    Event,
    Announcement
}
