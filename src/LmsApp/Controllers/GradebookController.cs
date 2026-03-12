using LmsApp.Data;
using LmsApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[Authorize]
public class GradebookController(LmsDbContext db) : Controller
{
    // GET: /Gradebook/Course/5  — instruktur melihat semua nilai
    [Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> Course(int courseId)
    {
        var course = await db.Courses.FindAsync(courseId);
        if (course is null) return NotFound();

        var enrollments = await db.Enrollments
            .Where(e => e.CourseId == courseId)
            .ToListAsync();

        var assignments = await db.Assignments
            .Where(a => a.CourseId == courseId)
            .ToListAsync();

        var quizzes = await db.Quizzes
            .Where(q => q.CourseId == courseId)
            .ToListAsync();

        var submissions = await db.Submissions
            .Where(s => assignments.Select(a => a.Id).Contains(s.AssignmentId))
            .ToListAsync();

        var quizAttempts = await db.QuizAttempts
            .Where(a => quizzes.Select(q => q.Id).Contains(a.QuizId))
            .ToListAsync();

        var vm = new GradebookViewModel
        {
            Course = course,
            Enrollments = enrollments,
            Assignments = assignments,
            Quizzes = quizzes,
            Submissions = submissions,
            QuizAttempts = quizAttempts
        };

        return View(vm);
    }

    // GET: /Gradebook/My/5  — peserta melihat nilai sendiri
    public async Task<IActionResult> My(int courseId)
    {
        var userId = User.FindFirst("sub")?.Value ?? string.Empty;

        var course = await db.Courses.FindAsync(courseId);
        if (course is null) return NotFound();

        var assignments = await db.Assignments
            .Include(a => a.Submissions.Where(s => s.UserId == userId))
            .Where(a => a.CourseId == courseId)
            .ToListAsync();

        var quizzes = await db.Quizzes
            .Include(q => q.Attempts.Where(a => a.UserId == userId))
            .Where(q => q.CourseId == courseId)
            .ToListAsync();

        var vm = new MyGradebookViewModel
        {
            Course = course,
            Assignments = assignments,
            Quizzes = quizzes,
            UserId = userId
        };

        return View(vm);
    }
}
