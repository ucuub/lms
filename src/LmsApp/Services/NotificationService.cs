using LmsApp.Data;
using LmsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Services;

// ── Interface ─────────────────────────────────────────────────────────────────

public interface INotificationService
{
    // Generic
    Task CreateAsync(string userId, string title, string message,
        NotificationType type = NotificationType.Info, string? link = null);

    // Typed helpers
    Task CreateForEnrollmentAsync(string userId, string courseTitle, int courseId);
    Task CreateForGradeAsync(string userId, string assignmentTitle, int score, string link);
    Task CreateForAnnouncementAsync(IEnumerable<string> userIds, string courseTitle,
        string announcementTitle, string link);

    /// <summary>Notif ke semua student saat quiz dipublish.</summary>
    Task CreateForQuizAvailableAsync(IEnumerable<string> userIds, string quizTitle,
        int courseId, int quizId);

    /// <summary>
    /// Reminder assignment due soon.
    /// Idempotent: tidak kirim ulang jika sudah dikirim (cek via link).
    /// </summary>
    Task CreateForAssignmentDueSoonAsync(string userId, string assignmentTitle,
        DateTime dueDate, string link);

    /// <summary>Notif saat sertifikat berhasil diterbitkan.</summary>
    Task CreateForCertificateAsync(string userId, string courseTitle, string certNumber);

    // Queries
    Task<int> GetUnreadCountAsync(string userId);
    Task MarkAllReadAsync(string userId);
}

// ── Implementation ────────────────────────────────────────────────────────────

public class NotificationService(LmsDbContext db) : INotificationService
{
    public async Task CreateAsync(string userId, string title, string message,
        NotificationType type = NotificationType.Info, string? link = null)
    {
        db.Notifications.Add(new Notification
        {
            UserId  = userId,
            Title   = title,
            Message = message,
            Type    = type,
            Link    = link
        });
        await db.SaveChangesAsync();
    }

    public async Task CreateForEnrollmentAsync(string userId, string courseTitle, int courseId)
        => await CreateAsync(userId,
            "Pendaftaran Berhasil",
            $"Kamu berhasil mendaftar kursus \"{courseTitle}\".",
            NotificationType.Success,
            $"/courses/{courseId}");

    public async Task CreateForGradeAsync(string userId, string assignmentTitle, int score, string link)
        => await CreateAsync(userId,
            "Tugas Dinilai",
            $"Tugas \"{assignmentTitle}\" telah dinilai. Nilai: {score}",
            NotificationType.Grade,
            link);

    public async Task CreateForAnnouncementAsync(IEnumerable<string> userIds,
        string courseTitle, string announcementTitle, string link)
    {
        var notifications = userIds.Select(uid => new Notification
        {
            UserId  = uid,
            Title   = $"Pengumuman: {courseTitle}",
            Message = announcementTitle,
            Type    = NotificationType.Announcement,
            Link    = link
        });
        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync();
    }

    public async Task CreateForQuizAvailableAsync(IEnumerable<string> userIds,
        string quizTitle, int courseId, int quizId)
    {
        var link = $"/courses/{courseId}/quizzes/{quizId}";
        var notifications = userIds.Select(uid => new Notification
        {
            UserId  = uid,
            Title   = "Quiz Tersedia",
            Message = $"Quiz \"{quizTitle}\" sudah bisa dikerjakan.",
            Type    = NotificationType.Quiz,
            Link    = link
        });
        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync();
    }

    public async Task CreateForAssignmentDueSoonAsync(string userId,
        string assignmentTitle, DateTime dueDate, string link)
    {
        // Idempotency: jangan kirim ulang jika sudah ada notif dengan link yang sama
        var alreadySent = await db.Notifications.AnyAsync(n =>
            n.UserId == userId && n.Link == link && n.Type == NotificationType.Assignment);
        if (alreadySent) return;

        var hoursLeft = (int)(dueDate - DateTime.UtcNow).TotalHours;
        var timeLabel = hoursLeft <= 1 ? "kurang dari 1 jam" : $"{hoursLeft} jam";

        await CreateAsync(userId,
            "Deadline Tugas Mendekat",
            $"Tugas \"{assignmentTitle}\" akan berakhir dalam {timeLabel}.",
            NotificationType.Assignment,
            link);
    }

    public async Task CreateForCertificateAsync(string userId, string courseTitle, string certNumber)
        => await CreateAsync(userId,
            "Sertifikat Diterima! 🎓",
            $"Selamat! Kamu mendapatkan sertifikat untuk \"{courseTitle}\".",
            NotificationType.Certificate,
            $"/certificates/{certNumber}");

    public async Task<int> GetUnreadCountAsync(string userId)
        => await db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task MarkAllReadAsync(string userId)
    {
        await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }
}
