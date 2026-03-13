using LmsApp.Data;
using LmsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[Authorize(Roles = "admin")]
public class AdminController(LmsDbContext db) : Controller
{
    // GET: /Admin
    public async Task<IActionResult> Index()
    {
        ViewBag.TotalCourses = await db.Courses.CountAsync();
        ViewBag.TotalEnrollments = await db.Enrollments.CountAsync();
        ViewBag.TotalQuizzes = await db.Quizzes.CountAsync();
        ViewBag.TotalCertificates = await db.Certificates.CountAsync();
        ViewBag.TotalUsers = await db.AppUsers.CountAsync();

        var recentCourses = await db.Courses
            .OrderByDescending(c => c.CreatedAt).Take(5).ToListAsync();

        return View(recentCourses);
    }

    // GET: /Admin/Courses
    public async Task<IActionResult> Courses()
    {
        var courses = await db.Courses
            .Include(c => c.Enrollments)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return View(courses);
    }

    // POST: /Admin/TogglePublish/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePublish(int id)
    {
        var course = await db.Courses.FindAsync(id);
        if (course is null) return NotFound();

        course.IsPublished = !course.IsPublished;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Kursus '{course.Title}' {(course.IsPublished ? "dipublikasikan" : "disembunyikan")}.";
        return RedirectToAction(nameof(Courses));
    }

    // POST: /Admin/DeleteCourse/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var course = await db.Courses.FindAsync(id);
        if (course is null) return NotFound();

        db.Courses.Remove(course);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Kursus '{course.Title}' dihapus.";
        return RedirectToAction(nameof(Courses));
    }

    // ── User Management ──────────────────────────────────────────────

    // GET: /Admin/Users
    public async Task<IActionResult> Users(string? search, string? role, int page = 1)
    {
        const int pageSize = 20;
        var query = db.AppUsers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.Name.Contains(search) || u.Email.Contains(search));

        if (!string.IsNullOrWhiteSpace(role) && role != "all")
            query = query.Where(u => u.Role == role);

        int total = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.LastLoginAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.FilterRole = role;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.Total = total;
        ViewBag.StudentCount = await db.AppUsers.CountAsync(u => u.Role == "student");
        ViewBag.InstructorCount = await db.AppUsers.CountAsync(u => u.Role == "instructor");
        ViewBag.AdminCount = await db.AppUsers.CountAsync(u => u.Role == "admin");
        return View(users);
    }

    // POST: /Admin/SetRole
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(int userId, string role)
    {
        var validRoles = new[] { "student", "instructor", "admin" };
        if (!validRoles.Contains(role))
        {
            TempData["Error"] = "Role tidak valid.";
            return RedirectToAction(nameof(Users));
        }

        var user = await db.AppUsers.FindAsync(userId);
        if (user is null) return NotFound();

        user.Role = role;
        await db.SaveChangesAsync();
        TempData["Success"] = $"Role {user.Name} diubah menjadi {role}.";
        return RedirectToAction(nameof(Users));
    }

    // POST: /Admin/ToggleUserActive
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserActive(int userId)
    {
        var user = await db.AppUsers.FindAsync(userId);
        if (user is null) return NotFound();

        user.IsActive = !user.IsActive;
        await db.SaveChangesAsync();
        TempData["Success"] = $"Akun {user.Name} {(user.IsActive ? "diaktifkan" : "dinonaktifkan")}.";
        return RedirectToAction(nameof(Users));
    }
}
