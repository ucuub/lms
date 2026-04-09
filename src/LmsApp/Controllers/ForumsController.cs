using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

/// <summary>
/// Discussion Forum — threaded, nested replies, likes, @mentions, moderation.
///
/// Routes:
///   GET    api/courses/{courseId}/forum                → thread list (paginated)
///   GET    api/courses/{courseId}/forum/{threadId}     → thread detail (nested replies)
///   POST   api/courses/{courseId}/forum                → create thread
///   POST   api/courses/{courseId}/forum/{threadId}/reply → reply (nested via ParentId)
///   PATCH  api/courses/{courseId}/forum/{postId}       → edit own post
///   DELETE api/courses/{courseId}/forum/{postId}       → soft-delete (own or teacher/admin)
///   POST   api/courses/{courseId}/forum/{threadId}/pin → toggle pin (teacher/admin)
///   POST   api/courses/{courseId}/forum/{postId}/like  → toggle like/upvote
/// </summary>
[ApiController]
[Route("api/courses/{courseId:int}/forum")]
[Authorize]
public class ForumsController(LmsDbContext db, IForumService forumService) : ControllerBase
{
    private string UserId   => User.FindFirst("sub")?.Value  ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";
    private string UserName => User.FindFirst("name")?.Value ?? string.Empty;

    // ── Thread List ───────────────────────────────────────────────────────────

    /// <summary>
    /// GET api/courses/{courseId}/forum?page=1&amp;pageSize=20&amp;search=keyword
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ForumThreadDto>>> GetThreads(
        int courseId,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        if (!await CanAccess(courseId)) return Forbid();

        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await forumService.GetThreadsAsync(courseId, UserId, page, pageSize, search);
        return Ok(result);
    }

    // ── Thread Detail ─────────────────────────────────────────────────────────

    /// <summary>
    /// GET api/courses/{courseId}/forum/{threadId}
    /// Returns the thread with full nested reply tree.
    /// </summary>
    [HttpGet("{threadId:int}")]
    public async Task<ActionResult<ForumThreadDetailDto>> GetThread(int courseId, int threadId)
    {
        if (!await CanAccess(courseId)) return Forbid();

        var thread = await forumService.GetThreadAsync(courseId, threadId, UserId);
        if (thread == null) return NotFound(new { message = "Thread tidak ditemukan." });

        return Ok(thread);
    }

    // ── Create Thread ─────────────────────────────────────────────────────────

    /// <summary>
    /// POST api/courses/{courseId}/forum
    /// Body: { "title": "...", "body": "..." }
    /// Supports @mention in body.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ForumThreadDto>> CreateThread(
        int courseId, CreateThreadRequest req)
    {
        if (!await CanAccess(courseId)) return Forbid();

        var thread = await forumService.CreateThreadAsync(courseId, UserId, UserName, req);
        return CreatedAtAction(nameof(GetThread),
            new { courseId, threadId = thread.Id }, thread);
    }

    // ── Reply ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// POST api/courses/{courseId}/forum/{threadId}/reply
    /// Body: { "body": "...", "parentId": null | &lt;replyId&gt; }
    /// ParentId = null  → direct reply to thread.
    /// ParentId = id    → nested reply (infinite depth).
    /// Supports @mention in body.
    /// </summary>
    [HttpPost("{threadId:int}/reply")]
    public async Task<ActionResult<ForumReplyNestedDto>> Reply(
        int courseId, int threadId, CreateReplyRequest req)
    {
        if (!await CanAccess(courseId)) return Forbid();

        try
        {
            var reply = await forumService.CreateReplyAsync(
                courseId, threadId, UserId, UserName, req);
            return Ok(reply);
        }
        catch (KeyNotFoundException e)        { return NotFound(new { message = e.Message }); }
        catch (InvalidOperationException e)   { return BadRequest(new { message = e.Message }); }
    }

    // ── Edit Post ─────────────────────────────────────────────────────────────

    /// <summary>
    /// PATCH api/courses/{courseId}/forum/{postId}
    /// Body: { "body": "..." }
    /// Owner can edit own post. Teacher/admin can edit any post.
    /// </summary>
    [HttpPatch("{postId:int}")]
    public async Task<IActionResult> UpdatePost(
        int courseId, int postId, UpdatePostRequest req)
    {
        try
        {
            await forumService.UpdatePostAsync(postId, UserId, UserRole, req);
            return NoContent();
        }
        catch (KeyNotFoundException e)        { return NotFound(new { message = e.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException e)   { return BadRequest(new { message = e.Message }); }
    }

    // ── Delete Post ───────────────────────────────────────────────────────────

    /// <summary>
    /// DELETE api/courses/{courseId}/forum/{postId}
    /// Soft-delete. Owner can delete own post. Teacher/admin can delete any post.
    /// Deleted posts remain in tree with placeholder body.
    /// </summary>
    [HttpDelete("{postId:int}")]
    public async Task<IActionResult> Delete(int courseId, int postId)
    {
        try
        {
            await forumService.DeletePostAsync(postId, courseId, UserId, UserRole);
            return NoContent();
        }
        catch (KeyNotFoundException e)        { return NotFound(new { message = e.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // ── Pin Thread ────────────────────────────────────────────────────────────

    /// <summary>
    /// POST api/courses/{courseId}/forum/{threadId}/pin
    /// Toggle pin status. Teacher/admin only.
    /// </summary>
    [HttpPost("{threadId:int}/pin")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> Pin(int courseId, int threadId)
    {
        try
        {
            var isPinned = await forumService.TogglePinAsync(threadId, courseId, UserRole);
            return Ok(new { isPinned });
        }
        catch (KeyNotFoundException e)        { return NotFound(new { message = e.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // ── Like / Upvote ─────────────────────────────────────────────────────────

    /// <summary>
    /// POST api/courses/{courseId}/forum/{postId}/like
    /// Toggle like on any post (thread or reply). Idempotent.
    /// Response: { "postId": 1, "likeCount": 5, "isLiked": true }
    /// </summary>
    [HttpPost("{postId:int}/like")]
    public async Task<ActionResult<LikeResultDto>> Like(int courseId, int postId)
    {
        if (!await CanAccess(courseId)) return Forbid();

        try
        {
            var result = await forumService.ToggleLikeAsync(postId, UserId);
            return Ok(result);
        }
        catch (KeyNotFoundException e) { return NotFound(new { message = e.Message }); }
    }

    // ── Access Control ────────────────────────────────────────────────────────

    private async Task<bool> CanAccess(int courseId)
    {
        if (UserRole is "teacher" or "admin") return true;
        return await db.Enrollments
            .AnyAsync(e => e.CourseId == courseId && e.UserId == UserId);
    }
}
