using LmsApp.Data;
using LmsApp.Models;
using LmsApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[Authorize]
public class CourseController(LmsDbContext db) : Controller
{
    // GET: /Course
    public async Task<IActionResult> Index(string? search)
    {
        var query = db.Courses.Where(c => c.IsPublished).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Title.Contains(search) || c.Description.Contains(search));

        var courses = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        ViewBag.Search = search;
        return View(courses);
    }

    // GET: /Course/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var course = await db.Courses
            .Include(c => c.Modules.OrderBy(m => m.Order))
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course is null) return NotFound();

        var userId = User.FindFirst("sub")?.Value ?? string.Empty;
        var isEnrolled = await db.Enrollments
            .AnyAsync(e => e.CourseId == id && e.UserId == userId);

        var vm = new CourseDetailsViewModel
        {
            Course = course,
            IsEnrolled = isEnrolled,
            EnrollmentCount = await db.Enrollments.CountAsync(e => e.CourseId == id)
        };

        return View(vm);
    }

    // POST: /Course/Enroll/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(int id)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (userId is null) return Unauthorized();

        var course = await db.Courses.FindAsync(id);
        if (course is null) return NotFound();

        var alreadyEnrolled = await db.Enrollments
            .AnyAsync(e => e.CourseId == id && e.UserId == userId);

        if (!alreadyEnrolled)
        {
            db.Enrollments.Add(new Enrollment
            {
                CourseId = id,
                UserId = userId,
                UserName = User.Identity?.Name ?? "Unknown"
            });
            await db.SaveChangesAsync();
            TempData["Success"] = "Berhasil mendaftar kursus!";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: /Course/Create  (instructor only)
    [Authorize(Roles = "instructor,admin")]
    public IActionResult Create() => View();

    // POST: /Course/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> Create(CourseCreateViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var course = new Course
        {
            Title = vm.Title,
            Description = vm.Description,
            InstructorId = User.FindFirst("sub")?.Value ?? string.Empty,
            InstructorName = User.Identity?.Name ?? string.Empty,
            IsPublished = vm.IsPublished
        };

        db.Courses.Add(course);
        await db.SaveChangesAsync();

        TempData["Success"] = "Kursus berhasil dibuat!";
        return RedirectToAction(nameof(Details), new { course.Id });
    }

    // GET: /Course/Edit/5
    [Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var course = await db.Courses.FindAsync(id);
        if (course is null) return NotFound();

        var userId = User.FindFirst("sub")?.Value;
        if (course.InstructorId != userId && !User.IsInRole("admin"))
            return Forbid();

        var vm = new CourseCreateViewModel
        {
            Title = course.Title,
            Description = course.Description,
            IsPublished = course.IsPublished
        };

        ViewBag.CourseId = id;
        return View(vm);
    }

    // POST: /Course/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> Edit(int id, CourseCreateViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var course = await db.Courses.FindAsync(id);
        if (course is null) return NotFound();

        course.Title = vm.Title;
        course.Description = vm.Description;
        course.IsPublished = vm.IsPublished;
        course.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        TempData["Success"] = "Kursus berhasil diperbarui!";
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: /Course/MyCourses
    public async Task<IActionResult> MyCourses()
    {
        var userId = User.FindFirst("sub")?.Value;
        if (userId is null) return Unauthorized();

        var enrollments = await db.Enrollments
            .Include(e => e.Course)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();

        return View(enrollments);
    }
}
