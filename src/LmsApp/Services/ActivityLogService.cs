using LmsApp.Data;
using LmsApp.Models;

namespace LmsApp.Services;

public class ActivityLogService(LmsDbContext db) : IActivityLogService
{
    public async Task LogAsync(string userId, string userName, string action,
        string entityType, int? entityId = null, string? entityTitle = null,
        int? courseId = null, string? metadata = null)
    {
        if (string.IsNullOrEmpty(userId)) return;
        db.ActivityLogs.Add(new ActivityLog
        {
            UserId = userId, UserName = userName,
            Action = action, EntityType = entityType,
            EntityId = entityId, EntityTitle = entityTitle,
            CourseId = courseId, Metadata = metadata,
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
