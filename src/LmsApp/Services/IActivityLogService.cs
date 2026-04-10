namespace LmsApp.Services;

public interface IActivityLogService
{
    Task LogAsync(string userId, string userName, string action,
        string entityType, int? entityId = null, string? entityTitle = null,
        int? courseId = null, string? metadata = null);
}
