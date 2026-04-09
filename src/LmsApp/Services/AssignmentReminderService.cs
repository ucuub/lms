using LmsApp.Data;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Services;

/// <summary>
/// Background service yang berjalan setiap jam.
/// Mencari assignment yang due dalam 24 jam ke depan dan mengirim reminder
/// ke student yang belum submit. Idempotent — tidak kirim ulang jika sudah dikirim.
/// </summary>
public class AssignmentReminderService(
    IServiceScopeFactory scopeFactory,
    ILogger<AssignmentReminderService> logger) : BackgroundService
{
    // Interval pengecekan: setiap 1 jam
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[ReminderService] Background service started.");

        // Delay singkat agar app selesai startup sebelum query pertama
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendDueSoonRemindersAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "[ReminderService] Error saat mengirim reminder.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task SendDueSoonRemindersAsync(CancellationToken ct)
    {
        // IServiceScopeFactory diperlukan karena DbContext dan INotificationService
        // bersifat Scoped, sedangkan BackgroundService bersifat Singleton.
        using var scope = scopeFactory.CreateScope();
        var db           = scope.ServiceProvider.GetRequiredService<LmsDbContext>();
        var notifService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now       = DateTime.UtcNow;
        var threshold = now.AddHours(24);

        // Assignment yang due dalam 24 jam ke depan (belum lewat deadline)
        var upcomingAssignments = await db.Assignments
            .Where(a => a.DueDate != null && a.DueDate > now && a.DueDate <= threshold)
            .Select(a => new { a.Id, a.CourseId, a.Title, DueDate = a.DueDate!.Value })
            .ToListAsync(ct);

        if (upcomingAssignments.Count == 0) return;

        logger.LogInformation("[ReminderService] Checking {Count} upcoming assignments.", upcomingAssignments.Count);

        foreach (var assignment in upcomingAssignments)
        {
            // Student yang enrolled di course ini
            var enrolledUserIds = await db.Enrollments
                .Where(e => e.CourseId == assignment.CourseId
                         && e.Status  == Models.EnrollmentStatus.Active)
                .Select(e => e.UserId)
                .ToListAsync(ct);

            // Exclude student yang sudah submit
            var submittedUserIds = await db.Submissions
                .Where(s => s.AssignmentId == assignment.Id)
                .Select(s => s.UserId)
                .ToHashSetAsync(ct);

            var pendingUserIds = enrolledUserIds
                .Where(uid => !submittedUserIds.Contains(uid))
                .ToList();

            if (pendingUserIds.Count == 0) continue;

            var link = $"/courses/{assignment.CourseId}/assignments/{assignment.Id}";

            foreach (var userId in pendingUserIds)
            {
                await notifService.CreateForAssignmentDueSoonAsync(
                    userId, assignment.Title, assignment.DueDate, link);
            }

            logger.LogInformation(
                "[ReminderService] Sent reminders for assignment {Id} to {Count} students.",
                assignment.Id, pendingUserIds.Count);
        }
    }
}

/// <summary>
/// Extension untuk HashSet async — tidak tersedia di EF Core secara default.
/// </summary>
internal static class QueryableExtensions
{
    internal static async Task<HashSet<T>> ToHashSetAsync<T>(
        this IQueryable<T> source, CancellationToken ct = default)
    {
        var list = await source.ToListAsync(ct);
        return list.ToHashSet();
    }
}
