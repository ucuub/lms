using LmsApp.Data;
using LmsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[Authorize(Roles = "instructor,admin")]
public class AssignmentController(LmsDbContext db) : Controller
{
    private string UserId => User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

    // GET /Assignment/Manage/{courseId}
    public async Task<IActionResult> Manage(int courseId)
    {
        var course = await db.Courses
            .Include(c => c.Assignments.OrderBy(a => a.CreatedAt))
                .ThenInclude(a => a.Submissions)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course is null) return NotFound();
        if (course.InstructorId != UserId && !User.IsInRole("admin")) return Forbid();

        return View(course);
    }

    // GET /Assignment/Create/{courseId}
    public async Task<IActionResult> Create(int courseId)
    {
        var course = await db.Courses.FindAsync(courseId);
        if (course is null) return NotFound();
        if (course.InstructorId != UserId && !User.IsInRole("admin")) return Forbid();
        ViewBag.Course = course;
        return View(new Assignment { CourseId = courseId, MaxScore = 100 });
    }

    // POST /Assignment/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Assignment model)
    {
        var course = await db.Courses.FindAsync(model.CourseId);
        if (course is null) return NotFound();
        if (course.InstructorId != UserId && !User.IsInRole("admin")) return Forbid();

        if (!ModelState.IsValid)
        {
            ViewBag.Course = course;
            return View(model);
        }

        db.Assignments.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Tugas berhasil dibuat.";
        return RedirectToAction(nameof(Manage), new { courseId = model.CourseId });
    }

    // GET /Assignment/Edit/{id}
    public async Task<IActionResult> Edit(int id)
    {
        var assignment = await db.Assignments.Include(a => a.Course).FirstOrDefaultAsync(a => a.Id == id);
        if (assignment is null) return NotFound();
        if (assignment.Course.InstructorId != UserId && !User.IsInRole("admin")) return Forbid();
        ViewBag.Course = assignment.Course;
        return View(assignment);
    }

    // POST /Assignment/Edit
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Assignment model)
    {
        var assignment = await db.Assignments.Include(a => a.Course).FirstOrDefaultAsync(a => a.Id == id);
        if (assignment is null) return NotFound();
        if (assignment.Course.InstructorId != UserId && !User.IsInRole("admin")) return Forbid();

        assignment.Title = model.Title;
        assignment.Description = model.Description;
        assignment.DueDate = model.DueDate;
        assignment.MaxScore = model.MaxScore;
        await db.SaveChangesAsync();

        TempData["Success"] = "Tugas berhasil diperbarui.";
        return RedirectToAction(nameof(Manage), new { courseId = assignment.CourseId });
    }

    // POST /Assignment/Delete
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var assignment = await db.Assignments.Include(a => a.Course).FirstOrDefaultAsync(a => a.Id == id);
        if (assignment is null) return NotFound();
        if (assignment.Course.InstructorId != UserId && !User.IsInRole("admin")) return Forbid();

        db.Assignments.Remove(assignment);
        await db.SaveChangesAsync();

        TempData["Success"] = "Tugas berhasil dihapus.";
        return RedirectToAction(nameof(Manage), new { courseId = assignment.CourseId });
    }
}
