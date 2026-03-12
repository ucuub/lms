using LmsApp.Data;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[Authorize]
public class NotificationController(LmsDbContext db, INotificationService notifService) : Controller
{
    // GET: /Notification
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst("sub")?.Value ?? string.Empty;
        var notifications = await db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        await notifService.MarkAllReadAsync(userId);
        return View(notifications);
    }

    // GET: /Notification/Count  — AJAX endpoint untuk bell icon
    public async Task<IActionResult> Count()
    {
        var userId = User.FindFirst("sub")?.Value ?? string.Empty;
        var count = await notifService.GetUnreadCountAsync(userId);
        return Json(new { count });
    }

    // POST: /Notification/MarkRead/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = User.FindFirst("sub")?.Value ?? string.Empty;
        var notif = await db.Notifications.FindAsync(id);
        if (notif is not null && notif.UserId == userId)
        {
            notif.IsRead = true;
            await db.SaveChangesAsync();
        }
        return Ok();
    }

    // POST: /Notification/MarkAllRead
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = User.FindFirst("sub")?.Value ?? string.Empty;
        await notifService.MarkAllReadAsync(userId);
        return RedirectToAction(nameof(Index));
    }
}
