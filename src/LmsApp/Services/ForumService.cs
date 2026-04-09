using System.Text.RegularExpressions;
using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Services;

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IForumService
{
    Task<PagedResponse<ForumThreadDto>> GetThreadsAsync(
        int courseId, string userId, int page, int pageSize, string? search);

    Task<ForumThreadDetailDto?> GetThreadAsync(int courseId, int threadId, string userId);

    Task<ForumThreadDto> CreateThreadAsync(
        int courseId, string userId, string userName, CreateThreadRequest req);

    Task<ForumReplyNestedDto> CreateReplyAsync(
        int courseId, int threadId, string userId, string userName, CreateReplyRequest req);

    Task UpdatePostAsync(int postId, string userId, string userRole, UpdatePostRequest req);

    Task DeletePostAsync(int postId, int courseId, string userId, string userRole);

    /// <returns>New IsPinned value.</returns>
    Task<bool> TogglePinAsync(int threadId, int courseId, string userRole);

    Task<LikeResultDto> ToggleLikeAsync(int postId, string userId);
}

// ── Implementation ────────────────────────────────────────────────────────────

public partial class ForumService(LmsDbContext db, INotificationService notifications) : IForumService
{
    [GeneratedRegex(@"@(\w+)", RegexOptions.Compiled)]
    private static partial Regex MentionRegex();

    // ── Threads ───────────────────────────────────────────────────────────────

    public async Task<PagedResponse<ForumThreadDto>> GetThreadsAsync(
        int courseId, string userId, int page, int pageSize, string? search)
    {
        var query = db.ForumPosts
            .Where(f => f.CourseId == courseId && f.ParentId == null && !f.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(f => f.Title.Contains(search) || f.Body.Contains(search));

        var total = await query.CountAsync();

        // Fetch ordered IDs first (avoids expensive ordering on large joins)
        var threadIds = await query
            .OrderByDescending(f => f.IsPinned)
            .ThenByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => f.Id)
            .ToListAsync();

        if (threadIds.Count == 0)
            return new PagedResponse<ForumThreadDto>([], total, page, pageSize, 0);

        // Load threads with their likes in one query
        var threads = await db.ForumPosts
            .Include(f => f.Likes)
            .Where(f => threadIds.Contains(f.Id))
            .ToListAsync();

        // Reply count + last-reply-at per thread (single aggregation query)
        var replyStats = await db.ForumPosts
            .Where(f => f.RootThreadId != null
                     && threadIds.Contains(f.RootThreadId.Value)
                     && !f.IsDeleted)
            .GroupBy(f => f.RootThreadId!.Value)
            .Select(g => new
            {
                ThreadId = g.Key,
                Count    = g.Count(),
                LastAt   = g.Max(r => r.CreatedAt)
            })
            .ToListAsync();

        // Which threads did current user like?
        var likedIds = (await db.ForumLikes
            .Where(l => threadIds.Contains(l.PostId) && l.UserId == userId)
            .Select(l => l.PostId)
            .ToListAsync()).ToHashSet();

        // Preserve the ordered sequence
        var dtos = threadIds.Select(tid =>
        {
            var t  = threads.First(x => x.Id == tid);
            var rc = replyStats.FirstOrDefault(r => r.ThreadId == tid);
            return new ForumThreadDto(
                t.Id, t.CourseId, t.UserId, t.UserName,
                t.Title, t.Body, t.IsPinned,
                rc?.Count ?? 0,
                t.Likes.Count,
                likedIds.Contains(t.Id),
                t.CreatedAt,
                rc?.LastAt
            );
        }).ToList();

        return new PagedResponse<ForumThreadDto>(
            dtos, total, page, pageSize,
            (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<ForumThreadDetailDto?> GetThreadAsync(int courseId, int threadId, string userId)
    {
        var thread = await db.ForumPosts
            .Include(f => f.Likes)
            .FirstOrDefaultAsync(f => f.Id == threadId
                                   && f.CourseId == courseId
                                   && f.ParentId == null);

        if (thread == null || thread.IsDeleted) return null;

        // Load ALL replies in this thread (any depth) via the denormalised RootThreadId
        var allReplies = await db.ForumPosts
            .Include(f => f.Likes)
            .Where(f => f.RootThreadId == threadId)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync();

        // One query for likes of the entire thread (thread + all replies)
        var allPostIds = allReplies.Select(r => r.Id).Append(threadId).ToHashSet();
        var likedIds = (await db.ForumLikes
            .Where(l => allPostIds.Contains(l.PostId) && l.UserId == userId)
            .Select(l => l.PostId)
            .ToListAsync()).ToHashSet();

        return new ForumThreadDetailDto(
            thread.Id, thread.CourseId, thread.UserId, thread.UserName,
            thread.Title, thread.Body, thread.IsPinned,
            thread.Likes.Count,
            likedIds.Contains(thread.Id),
            thread.CreatedAt,
            thread.EditedAt,
            BuildTree(allReplies, threadId, likedIds)
        );
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<ForumThreadDto> CreateThreadAsync(
        int courseId, string userId, string userName, CreateThreadRequest req)
    {
        var thread = new ForumPost
        {
            CourseId = courseId,
            UserId   = userId,
            UserName = userName,
            Title    = req.Title,
            Body     = req.Body
            // ParentId = null, RootThreadId = null → this IS the root
        };
        db.ForumPosts.Add(thread);
        await db.SaveChangesAsync();

        await ProcessMentionsAsync(thread, courseId);

        return new ForumThreadDto(
            thread.Id, thread.CourseId, thread.UserId, thread.UserName,
            thread.Title, thread.Body, false, 0, 0, false, thread.CreatedAt, null);
    }

    public async Task<ForumReplyNestedDto> CreateReplyAsync(
        int courseId, int threadId,
        string userId, string userName,
        CreateReplyRequest req)
    {
        var thread = await db.ForumPosts
            .FirstOrDefaultAsync(f => f.Id == threadId
                                   && f.CourseId == courseId
                                   && f.ParentId == null
                                   && !f.IsDeleted)
            ?? throw new KeyNotFoundException("Thread tidak ditemukan.");

        // Determine actual parent (direct reply to thread or nested reply)
        int actualParentId = threadId;
        if (req.ParentId.HasValue)
        {
            var parent = await db.ForumPosts.FindAsync(req.ParentId.Value)
                ?? throw new KeyNotFoundException("Post induk tidak ditemukan.");

            // Guard: parent must belong to this thread
            if (parent.RootThreadId != threadId && parent.Id != threadId)
                throw new InvalidOperationException("Post induk bukan bagian dari thread ini.");

            if (parent.IsDeleted)
                throw new InvalidOperationException("Tidak bisa membalas post yang dihapus.");

            actualParentId = req.ParentId.Value;
        }

        var reply = new ForumPost
        {
            CourseId     = courseId,
            ParentId     = actualParentId,
            RootThreadId = threadId,
            UserId       = userId,
            UserName     = userName,
            Title        = string.Empty,
            Body         = req.Body
        };
        db.ForumPosts.Add(reply);
        await db.SaveChangesAsync();

        // Notify thread owner (skip if replying to own thread)
        if (thread.UserId != userId)
        {
            await notifications.CreateAsync(
                thread.UserId,
                "Balasan Baru di Forum",
                $"{userName} membalas thread \"{thread.Title}\".",
                NotificationType.Info,
                $"/courses/{courseId}/forum/{threadId}"
            );
        }

        await ProcessMentionsAsync(reply, courseId, threadId);

        return new ForumReplyNestedDto(
            reply.Id, threadId, reply.ParentId,
            reply.UserId, reply.UserName,
            reply.Body, false, 0, false,
            reply.CreatedAt, null, []
        );
    }

    // ── Edit / Delete ─────────────────────────────────────────────────────────

    public async Task UpdatePostAsync(int postId, string userId, string userRole, UpdatePostRequest req)
    {
        var post = await db.ForumPosts.FindAsync(postId)
            ?? throw new KeyNotFoundException("Post tidak ditemukan.");

        if (post.IsDeleted)
            throw new InvalidOperationException("Post yang dihapus tidak bisa diedit.");

        if (post.UserId != userId && userRole is not ("teacher" or "admin"))
            throw new UnauthorizedAccessException("Kamu tidak berhak mengedit post ini.");

        post.Body      = req.Body;
        post.EditedAt  = DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DeletePostAsync(int postId, int courseId, string userId, string userRole)
    {
        var post = await db.ForumPosts.FindAsync(postId)
            ?? throw new KeyNotFoundException("Post tidak ditemukan.");

        if (post.CourseId != courseId)
            throw new KeyNotFoundException("Post tidak ditemukan.");

        if (post.UserId != userId && userRole is not ("teacher" or "admin"))
            throw new UnauthorizedAccessException("Kamu tidak berhak menghapus post ini.");

        post.IsDeleted = true;
        post.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    // ── Pin ───────────────────────────────────────────────────────────────────

    public async Task<bool> TogglePinAsync(int threadId, int courseId, string userRole)
    {
        if (userRole is not ("teacher" or "admin"))
            throw new UnauthorizedAccessException("Hanya teacher/admin yang bisa pin thread.");

        var thread = await db.ForumPosts
            .FirstOrDefaultAsync(f => f.Id == threadId
                                   && f.CourseId == courseId
                                   && f.ParentId == null)
            ?? throw new KeyNotFoundException("Thread tidak ditemukan.");

        thread.IsPinned = !thread.IsPinned;
        await db.SaveChangesAsync();
        return thread.IsPinned;
    }

    // ── Like / Upvote ─────────────────────────────────────────────────────────

    public async Task<LikeResultDto> ToggleLikeAsync(int postId, string userId)
    {
        var post = await db.ForumPosts
            .FirstOrDefaultAsync(f => f.Id == postId && !f.IsDeleted)
            ?? throw new KeyNotFoundException("Post tidak ditemukan.");

        var existing = await db.ForumLikes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

        if (existing != null)
            db.ForumLikes.Remove(existing);
        else
            db.ForumLikes.Add(new ForumLike { PostId = postId, UserId = userId });

        await db.SaveChangesAsync();

        var newCount = await db.ForumLikes.CountAsync(l => l.PostId == postId);
        return new LikeResultDto(postId, newCount, existing == null);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Recursively builds a nested reply tree from a flat list (in-memory).
    /// Deleted posts are kept as placeholders so thread structure stays intact.
    /// </summary>
    private static List<ForumReplyNestedDto> BuildTree(
        List<ForumPost> allPosts, int parentId, HashSet<int> likedIds)
    {
        return allPosts
            .Where(p => p.ParentId == parentId)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new ForumReplyNestedDto(
                p.Id,
                p.RootThreadId ?? parentId,
                p.ParentId,
                p.IsDeleted ? string.Empty : p.UserId,
                p.IsDeleted ? "[dihapus]" : p.UserName,
                p.IsDeleted ? "[Pesan ini telah dihapus]" : p.Body,
                p.IsDeleted,
                p.Likes.Count,
                likedIds.Contains(p.Id),
                p.CreatedAt,
                p.EditedAt,
                BuildTree(allPosts, p.Id, likedIds)   // recurse
            ))
            .ToList();
    }

    /// <summary>
    /// Parses @username mentions from post body and sends in-app notifications.
    /// </summary>
    private async Task ProcessMentionsAsync(ForumPost post, int courseId, int? rootThreadId = null)
    {
        var matches = MentionRegex().Matches(post.Body);
        if (matches.Count == 0) return;

        var names = matches
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var mentionedUsers = await db.AppUsers
            .Where(u => names.Contains(u.Name) && u.UserId != post.UserId)
            .ToListAsync();

        var threadLink = $"/courses/{courseId}/forum/{rootThreadId ?? post.Id}";

        foreach (var user in mentionedUsers)
        {
            await notifications.CreateAsync(
                user.UserId,
                "Kamu Disebutkan di Forum",
                $"{post.UserName} menyebut kamu dalam diskusi forum.",
                NotificationType.Info,
                threadLink
            );
        }
    }
}
