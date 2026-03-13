using LmsApp.Data;
using LmsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[Authorize]
public class ForumController(LmsDbContext db) : Controller
{
    private string UserId => User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
    private string UserName => User.FindFirst("name")?.Value ?? User.Identity?.Name ?? "Pengguna";

    // GET /Forum/Index/{courseId}  — daftar thread
    public async Task<IActionResult> Index(int courseId, int page = 1)
    {
        var course = await db.Courses.FindAsync(courseId);
        if (course is null) return NotFound();

        // Check access: enrolled or instructor/admin
        bool isInstructor = User.IsInRole("instructor") || User.IsInRole("admin");
        if (!isInstructor)
        {
            bool enrolled = await db.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserId == UserId);
            if (!enrolled) return Forbid();
        }

        const int pageSize = 15;
        var query = db.ForumPosts
            .Where(f => f.CourseId == courseId && f.ParentId == null && !f.IsDeleted)
            .OrderByDescending(f => f.IsPinned)
            .ThenByDescending(f => f.UpdatedAt);

        int total = await query.CountAsync();
        var threads = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new
            {
                f.Id, f.Title, f.UserName, f.CreatedAt, f.UpdatedAt, f.IsPinned,
                ReplyCount = db.ForumPosts.Count(r => r.ParentId == f.Id && !r.IsDeleted)
            })
            .ToListAsync();

        ViewBag.Course = course;
        ViewBag.Threads = threads;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.IsInstructor = isInstructor;
        ViewBag.UserId = UserId;
        return View();
    }

    // GET /Forum/Thread/{id}
    public async Task<IActionResult> Thread(int id)
    {
        var thread = await db.ForumPosts
            .Include(f => f.Course)
            .Include(f => f.Replies.Where(r => !r.IsDeleted))
            .FirstOrDefaultAsync(f => f.Id == id && f.ParentId == null);

        if (thread is null || thread.IsDeleted) return NotFound();

        bool isInstructor = User.IsInRole("instructor") || User.IsInRole("admin");
        if (!isInstructor)
        {
            bool enrolled = await db.Enrollments.AnyAsync(e => e.CourseId == thread.CourseId && e.UserId == UserId);
            if (!enrolled) return Forbid();
        }

        ViewBag.IsInstructor = isInstructor;
        ViewBag.UserId = UserId;
        return View(thread);
    }

    // GET /Forum/Create/{courseId}
    public async Task<IActionResult> Create(int courseId)
    {
        var course = await db.Courses.FindAsync(courseId);
        if (course is null) return NotFound();
        ViewBag.Course = course;
        return View();
    }

    // POST /Forum/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int courseId, string title, string body)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
        {
            TempData["Error"] = "Judul dan isi diskusi wajib diisi.";
            return RedirectToAction(nameof(Create), new { courseId });
        }

        var post = new ForumPost
        {
            CourseId = courseId,
            UserId = UserId,
            UserName = UserName,
            Title = title.Trim(),
            Body = body.Trim()
        };
        db.ForumPosts.Add(post);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Thread), new { id = post.Id });
    }

    // POST /Forum/Reply
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int threadId, string body)
    {
        var thread = await db.ForumPosts.FindAsync(threadId);
        if (thread is null || thread.IsDeleted) return NotFound();

        if (!string.IsNullOrWhiteSpace(body))
        {
            var reply = new ForumPost
            {
                CourseId = thread.CourseId,
                ParentId = threadId,
                UserId = UserId,
                UserName = UserName,
                Title = string.Empty,
                Body = body.Trim()
            };
            db.ForumPosts.Add(reply);

            // Bump thread UpdatedAt so it sorts to top
            thread.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Thread), new { id = threadId });
    }

    // POST /Forum/Delete
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var post = await db.ForumPosts.FindAsync(id);
        if (post is null) return NotFound();

        bool canDelete = post.UserId == UserId || User.IsInRole("instructor") || User.IsInRole("admin");
        if (!canDelete) return Forbid();

        post.IsDeleted = true;
        post.Body = "[Pesan telah dihapus]";
        await db.SaveChangesAsync();

        if (post.ParentId is null)
            return RedirectToAction(nameof(Index), new { courseId = post.CourseId });
        return RedirectToAction(nameof(Thread), new { id = post.ParentId });
    }

    // POST /Forum/Pin  (instructor only)
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> Pin(int id)
    {
        var post = await db.ForumPosts.FindAsync(id);
        if (post is null) return NotFound();
        post.IsPinned = !post.IsPinned;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { courseId = post.CourseId });
    }
}
