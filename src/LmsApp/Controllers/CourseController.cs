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
    public async Task<IActionResult> Index(string? search, string? category, string? level, string? sort)
    {
        var query = db.Courses
            .Where(c => c.IsPublished)
            .Include(c => c.Enrollments)
            .Include(c => c.Reviews)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Title.Contains(search)
                               || c.Description.Contains(search)
                               || c.InstructorName.Contains(search));

        if (!string.IsNullOrWhiteSpace(category) && category != "all")
            query = query.Where(c => c.Category == category);

        if (!string.IsNullOrWhiteSpace(level) && level != "all")
            query = query.Where(c => c.Level == level);

        var courses = await query.ToListAsync();

        // Sort in-memory (Reviews average can't easily translate to SQL in some providers)
        courses = sort switch
        {
            "popular" => courses.OrderByDescending(c => c.Enrollments.Count).ToList(),
            "rating"  => courses.OrderByDescending(c => c.Reviews.Any() ? c.Reviews.Average(r => r.Rating) : 0).ToList(),
            "oldest"  => courses.OrderBy(c => c.CreatedAt).ToList(),
            _         => courses.OrderByDescending(c => c.CreatedAt).ToList()
        };

        var categories = await db.Courses
            .Where(c => c.IsPublished && c.Category != null)
            .Select(c => c.Category!).Distinct().OrderBy(c => c).ToListAsync();

        ViewBag.Search     = search;
        ViewBag.Category   = category;
        ViewBag.Level      = level;
        ViewBag.Sort       = sort;
        ViewBag.Categories = categories;
        return View(courses);
    }

    // GET: /Course/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var course = await db.Courses
            .Include(c => c.Modules.OrderBy(m => m.Order))
            .Include(c => c.Assignments)
            .Include(c => c.Reviews.OrderByDescending(r => r.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course is null) return NotFound();

        var userId = User.FindFirst("sub")?.Value ?? string.Empty;
        var isEnrolled = await db.Enrollments
            .AnyAsync(e => e.CourseId == id && e.UserId == userId);

        var userReview = course.Reviews.FirstOrDefault(r => r.UserId == userId);

        var vm = new CourseDetailsViewModel
        {
            Course = course,
            IsEnrolled = isEnrolled,
            EnrollmentCount = await db.Enrollments.CountAsync(e => e.CourseId == id)
        };

        ViewBag.UserReview  = userReview;
        ViewBag.AvgRating   = course.Reviews.Any() ? Math.Round(course.Reviews.Average(r => r.Rating), 1) : (double?)null;
        ViewBag.ReviewCount = course.Reviews.Count;
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
            Category = vm.Category,
            Level = vm.Level,
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
        course.Category = vm.Category;
        course.Level = vm.Level;
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
