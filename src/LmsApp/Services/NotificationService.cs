using LmsApp.Data;
using LmsApp.Models;

namespace LmsApp.Services;

public interface INotificationService
{
    Task CreateAsync(string userId, string title, string message, NotificationType type = NotificationType.Info, string? link = null);
    Task CreateForEnrollmentAsync(string userId, string courseTitle, int courseId);
    Task CreateForGradeAsync(string userId, string assignmentTitle, int score, string link);
    Task CreateForAnnouncementAsync(IEnumerable<string> userIds, string courseTitle, string announcementTitle, string link);
    Task<int> GetUnreadCountAsync(string userId);
    Task MarkAllReadAsync(string userId);
}

public class NotificationService(LmsDbContext db) : INotificationService
{
    public async Task CreateAsync(string userId, string title, string message,
        NotificationType type = NotificationType.Info, string? link = null)
    {
        db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            Link = link
        });
        await db.SaveChangesAsync();
    }

    public async Task CreateForEnrollmentAsync(string userId, string courseTitle, int courseId)
        => await CreateAsync(userId,
            "Pendaftaran Berhasil",
            $"Kamu berhasil mendaftar kursus \"{courseTitle}\".",
            NotificationType.Success,
            $"/Course/Details/{courseId}");

    public async Task CreateForGradeAsync(string userId, string assignmentTitle, int score, string link)
        => await CreateAsync(userId,
            "Tugas Dinilai",
            $"Tugas \"{assignmentTitle}\" kamu telah dinilai. Nilai: {score}",
            NotificationType.Grade,
            link);

    public async Task CreateForAnnouncementAsync(IEnumerable<string> userIds, string courseTitle,
        string announcementTitle, string link)
    {
        var notifications = userIds.Select(uid => new Notification
        {
            UserId = uid,
            Title = $"Pengumuman: {courseTitle}",
            Message = announcementTitle,
            Type = NotificationType.Announcement,
            Link = link
        });
        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
        => await Task.FromResult(db.Notifications.Count(n => n.UserId == userId && !n.IsRead));

    public async Task MarkAllReadAsync(string userId)
    {
        var unread = db.Notifications.Where(n => n.UserId == userId && !n.IsRead);
        foreach (var n in unread) n.IsRead = true;
        await db.SaveChangesAsync();
    }
}
