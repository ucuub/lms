using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[ApiController]
[Route("api/courses/{courseId:int}/forum")]
[Authorize]
public class ForumsController(LmsDbContext db) : ControllerBase
{
    private string UserId => User.FindFirst("sub")?.Value ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";
    private string UserName => User.FindFirst("name")?.Value ?? string.Empty;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ForumThreadDto>>> GetThreads(
        int courseId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!await CanAccess(courseId)) return Forbid();

        var query = db.ForumPosts
            .Where(f => f.CourseId == courseId && f.ParentId == null && !f.IsDeleted)
            .Include(f => f.Replies);

        var total = await query.CountAsync();
        var threads = await query
            .OrderByDescending(f => f.IsPinned)
            .ThenByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResponse<ForumThreadDto>(
            threads.Select(t => new ForumThreadDto(
                t.Id, t.CourseId, t.UserId, t.UserName,
                t.Title ?? string.Empty, t.Body,
                t.IsPinned, t.Replies.Count(r => !r.IsDeleted),
                t.CreatedAt)),
            total, page, pageSize, (int)Math.Ceiling(total / (double)pageSize)));
    }

    [HttpGet("{threadId:int}")]
    public async Task<ActionResult<ForumThreadDetailDto>> GetThread(int courseId, int threadId)
    {
        if (!await CanAccess(courseId)) return Forbid();

        var thread = await db.ForumPosts
            .Include(f => f.Replies.Where(r => !r.IsDeleted).OrderBy(r => r.CreatedAt))
            .FirstOrDefaultAsync(f => f.Id == threadId && f.CourseId == courseId && f.ParentId == null);

        if (thread == null || thread.IsDeleted) return NotFound();

        return Ok(new ForumThreadDetailDto(
            thread.Id, thread.CourseId, thread.UserId, thread.UserName,
            thread.Title ?? string.Empty, thread.Body, thread.IsPinned, thread.CreatedAt,
            thread.Replies.Select(r => new ForumReplyDto(
                r.Id, thread.Id, r.UserId, r.UserName, r.Body, r.CreatedAt)).ToList()));
    }

    [HttpPost]
    public async Task<ActionResult<ForumThreadDto>> CreateThread(int courseId, CreateThreadRequest req)
    {
        if (!await CanAccess(courseId)) return Forbid();

        var thread = new ForumPost
        {
            CourseId = courseId,
            UserId = UserId,
            UserName = UserName,
            Title = req.Title,
            Body = req.Body
        };
        db.ForumPosts.Add(thread);
        await db.SaveChangesAsync();

        return Ok(new ForumThreadDto(
            thread.Id, thread.CourseId, thread.UserId, thread.UserName,
            thread.Title!, thread.Body, thread.IsPinned, 0, thread.CreatedAt));
    }

    [HttpPost("{threadId:int}/reply")]
    public async Task<ActionResult<ForumReplyDto>> Reply(int courseId, int threadId, CreateReplyRequest req)
    {
        if (!await CanAccess(courseId)) return Forbid();

        var thread = await db.ForumPosts.FindAsync(threadId);
        if (thread == null || thread.IsDeleted || thread.CourseId != courseId) return NotFound();

        var reply = new ForumPost
        {
            CourseId = courseId,
            ParentId = threadId,
            UserId = UserId,
            UserName = UserName,
            Title = string.Empty,
            Body = req.Body
        };
        db.ForumPosts.Add(reply);
        await db.SaveChangesAsync();

        return Ok(new ForumReplyDto(reply.Id, threadId, reply.UserId, reply.UserName, reply.Body, reply.CreatedAt));
    }

    [HttpDelete("{postId:int}")]
    public async Task<IActionResult> Delete(int courseId, int postId)
    {
        var post = await db.ForumPosts.FindAsync(postId);
        if (post == null || post.CourseId != courseId) return NotFound();

        var isTeacher = UserRole is "teacher" or "admin";
        if (post.UserId != UserId && !isTeacher) return Forbid();

        post.IsDeleted = true;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{threadId:int}/pin")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> Pin(int courseId, int threadId)
    {
        var thread = await db.ForumPosts.FindAsync(threadId);
        if (thread == null || thread.CourseId != courseId || thread.ParentId != null) return NotFound();

        thread.IsPinned = !thread.IsPinned;
        await db.SaveChangesAsync();
        return Ok(new { isPinned = thread.IsPinned });
    }

    private async Task<bool> CanAccess(int courseId)
    {
        if (UserRole is "teacher" or "admin") return true;
        return await db.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserId == UserId);
    }
}
