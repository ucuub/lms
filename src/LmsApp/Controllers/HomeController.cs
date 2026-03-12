using LmsApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

public class HomeController(LmsDbContext db, ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        var totalCourses = await db.Courses.CountAsync(c => c.IsPublished);
        var totalEnrollments = await db.Enrollments.CountAsync();

        ViewBag.TotalCourses = totalCourses;
        ViewBag.TotalEnrollments = totalEnrollments;

        var featuredCourses = await db.Courses
            .Where(c => c.IsPublished)
            .OrderByDescending(c => c.CreatedAt)
            .Take(6)
            .ToListAsync();

        return View(featuredCourses);
    }

    [Authorize]
    public IActionResult Dashboard()
    {
        return View();
    }

    public IActionResult Error()
    {
        return View();
    }
}
