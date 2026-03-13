using LmsApp.Data;
using LmsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Services;

/// <summary>
/// Registers / updates an AppUser row on every login.
/// Called from AccountController after OIDC authentication succeeds.
/// </summary>
public interface IUserSyncService
{
    Task SyncAsync(string userId, string name, string email);
}

public class UserSyncService(LmsDbContext db) : IUserSyncService
{
    public async Task SyncAsync(string userId, string name, string email)
    {
        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user is null)
        {
            db.AppUsers.Add(new AppUser
            {
                UserId = userId,
                Name = name,
                Email = email,
                Role = "student"
            });
        }
        else
        {
            user.Name = name;
            user.Email = email;
            user.LastLoginAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
    }
}
