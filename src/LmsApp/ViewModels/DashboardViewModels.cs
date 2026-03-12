using LmsApp.Models;

namespace LmsApp.ViewModels;

public class DashboardViewModel
{
    public IList<Enrollment> Enrollments { get; set; } = [];
    public IList<CourseProgress> Progresses { get; set; } = [];
    public IList<CalendarEvent> UpcomingDeadlines { get; set; } = [];
    public IList<Announcement> RecentAnnouncements { get; set; } = [];
    public IList<Certificate> Certificates { get; set; } = [];

    public CourseProgress? GetProgress(int courseId) =>
        Progresses.FirstOrDefault(p => p.CourseId == courseId);
}
