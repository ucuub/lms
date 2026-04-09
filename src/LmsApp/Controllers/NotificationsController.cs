using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

/// <summary>
/// Notification center — in-app notifications dengan read/unread tracking.
///
/// Routes:
///   GET    api/notifications               → list (paginated, filter type/unread)
///   GET    api/notifications/count         → jumlah unread
///   POST   api/notifications/{id}/read     → tandai satu sebagai dibaca
///   POST   api/notifications/read-all      → tandai semua sebagai dibaca
///   DELETE api/notifications/{id}          → hapus satu notifikasi
///   DELETE api/notifications/read          → hapus semua yang sudah dibaca
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(LmsDbContext db, INotificationService notifService) : ControllerBase
{
    private string UserId => User.FindFirst("sub")?.Value ?? string.Empty;

    // ── List ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// GET api/notifications?page=1&amp;pageSize=20&amp;unreadOnly=false&amp;type=Assignment
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<NotificationListResponse>> GetAll(
        [FromQuery] int     page       = 1,
        [FromQuery] int     pageSize   = 20,
        [FromQuery] bool    unreadOnly = false,
        [FromQuery] string? type       = null)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Notifications.Where(n => n.UserId == UserId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        if (!string.IsNullOrWhiteSpace(type)
            && Enum.TryParse<NotificationType>(type, ignoreCase: true, out var parsedType))
            query = query.Where(n => n.Type == parsedType);

        var total       = await query.CountAsync();
        var unreadCount = await db.Notifications.CountAsync(n => n.UserId == UserId && !n.IsRead);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto(
                n.Id, n.Title, n.Message, n.Link,
                n.Type.ToString(), n.IsRead, n.CreatedAt))
            .ToListAsync();

        return Ok(new NotificationListResponse(
            items, total, unreadCount, page, pageSize,
            (int)Math.Ceiling(total / (double)pageSize)));
    }

    // ── Count ─────────────────────────────────────────────────────────────────

    /// <summary>GET api/notifications/count → { "count": 5 }</summary>
    [HttpGet("count")]
    public async Task<IActionResult> GetCount()
    {
        var count = await notifService.GetUnreadCountAsync(UserId);
        return Ok(new { count });
    }

    // ── Mark Read ─────────────────────────────────────────────────────────────

    /// <summary>POST api/notifications/{id}/read</summary>
    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var n = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == UserId);
        if (n == null) return NotFound();

        n.IsRead = true;
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>POST api/notifications/read-all</summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await notifService.MarkAllReadAsync(UserId);
        return NoContent();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    /// <summary>DELETE api/notifications/{id}</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var n = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == UserId);
        if (n == null) return NotFound();

        db.Notifications.Remove(n);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// DELETE api/notifications/read
    /// Hapus semua notifikasi yang sudah dibaca (bulk cleanup).
    /// </summary>
    [HttpDelete("read")]
    public async Task<IActionResult> DeleteAllRead()
    {
        var deleted = await db.Notifications
            .Where(n => n.UserId == UserId && n.IsRead)
            .ExecuteDeleteAsync();

        return Ok(new { deleted });
    }
}
