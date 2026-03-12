using LmsApp.Data;
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
}
