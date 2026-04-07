using LmsApp.Data;
using LmsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(LmsDbContext db) : ControllerBase
{
    private string UserId => User.FindFirst("userId")?.Value ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = db.Notifications.Where(n => n.UserId == UserId);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new { items, total, unreadCount = items.Count(n => !n.IsRead) });
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetCount()
    {
        var count = await db.Notifications.CountAsync(n => n.UserId == UserId && !n.IsRead);
        return Ok(new { count });
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var n = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == UserId);
        if (n == null) return NotFound();
        n.IsRead = true;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await db.Notifications
            .Where(n => n.UserId == UserId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var n = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == UserId);
        if (n == null) return NotFound();
        db.Notifications.Remove(n);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
