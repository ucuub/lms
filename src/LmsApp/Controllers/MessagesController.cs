using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[ApiController]
[Route("api/messages")]
[Authorize]
public class MessagesController(LmsDbContext db) : ControllerBase
{
    private string UserId   => User.FindFirst("sub")?.Value  ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";
    private string UserName => User.FindFirst("name")?.Value ?? string.Empty;

    // GET /api/messages/conversations
    [HttpGet("conversations")]
    public async Task<ActionResult<IEnumerable<ConversationDto>>> GetConversations()
    {
        var convs = await db.Conversations
            .Include(c => c.Messages)
            .Where(c => c.User1Id == UserId || c.User2Id == UserId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();

        return Ok(convs.Select(c => ToConversationDto(c)));
    }

    // GET /api/messages/{conversationId}
    [HttpGet("{conversationId:int}")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessages(int conversationId)
    {
        var conv = await db.Conversations.FindAsync(conversationId);
        if (conv == null) return NotFound();
        if (conv.User1Id != UserId && conv.User2Id != UserId) return Forbid();

        var messages = await db.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        // Mark unread messages as read
        var unread = messages.Where(m => m.SenderId != UserId && !m.IsRead).ToList();
        unread.ForEach(m => m.IsRead = true);
        if (unread.Any()) await db.SaveChangesAsync();

        return Ok(messages.Select(ToMessageDto));
    }

    // POST /api/messages/send
    [HttpPost("send")]
    public async Task<ActionResult<MessageDto>> Send([FromBody] SendMessageRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Content)) return BadRequest(new { message = "Pesan tidak boleh kosong." });
        if (req.RecipientId == UserId) return BadRequest(new { message = "Tidak bisa kirim pesan ke diri sendiri." });

        // Find or create conversation
        var conv = await db.Conversations.FirstOrDefaultAsync(c =>
            (c.User1Id == UserId && c.User2Id == req.RecipientId) ||
            (c.User1Id == req.RecipientId && c.User2Id == UserId));

        if (conv == null)
        {
            // Coba ambil nama recipient dari AppUsers berdasarkan UserId (bukan PK)
            var recipientUser = await db.AppUsers.FirstOrDefaultAsync(u => u.UserId == req.RecipientId);
            var recipientName = req.RecipientName ?? recipientUser?.Name ?? req.RecipientId;

            conv = new Conversation
            {
                User1Id = UserId, User1Name = UserName,
                User2Id = req.RecipientId, User2Name = recipientName
            };
            db.Conversations.Add(conv);
            await db.SaveChangesAsync();
        }

        var msg = new Message
        {
            ConversationId = conv.Id,
            SenderId = UserId,
            SenderName = UserName,
            Content = req.Content.Trim()
        };
        db.Messages.Add(msg);
        conv.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMessages), new { conversationId = conv.Id }, ToMessageDto(msg));
    }

    // POST /api/messages/{conversationId}/read  — mark all messages in conv as read
    [HttpPost("{conversationId:int}/read")]
    public async Task<IActionResult> MarkRead(int conversationId)
    {
        var conv = await db.Conversations.FindAsync(conversationId);
        if (conv == null) return NotFound();
        if (conv.User1Id != UserId && conv.User2Id != UserId) return Forbid();

        var unread = await db.Messages
            .Where(m => m.ConversationId == conversationId && m.SenderId != UserId && !m.IsRead)
            .ToListAsync();
        unread.ForEach(m => m.IsRead = true);
        if (unread.Any()) await db.SaveChangesAsync();

        return NoContent();
    }

    // GET /api/messages/unread-count
    [HttpGet("unread-count")]
    public async Task<ActionResult<object>> UnreadCount()
    {
        var convIds = await db.Conversations
            .Where(c => c.User1Id == UserId || c.User2Id == UserId)
            .Select(c => c.Id)
            .ToListAsync();

        var count = await db.Messages
            .Where(m => convIds.Contains(m.ConversationId) && m.SenderId != UserId && !m.IsRead)
            .CountAsync();

        return Ok(new { count });
    }

    private ConversationDto ToConversationDto(Conversation c)
    {
        var otherUserId   = c.User1Id == UserId ? c.User2Id   : c.User1Id;
        var otherUserName = c.User1Id == UserId ? c.User2Name : c.User1Name;
        var last = c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
        var unread = c.Messages.Count(m => m.SenderId != UserId && !m.IsRead);
        return new ConversationDto(c.Id, otherUserId, otherUserName,
            last?.Content, last?.CreatedAt, unread, c.CreatedAt);
    }

    private static MessageDto ToMessageDto(Message m) =>
        new(m.Id, m.ConversationId, m.SenderId, m.SenderName, m.Content, m.IsRead, m.CreatedAt);
}
