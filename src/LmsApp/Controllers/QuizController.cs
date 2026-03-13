using LmsApp.Data;
using LmsApp.Models;
using LmsApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[Authorize]
public class QuizController(LmsDbContext db) : Controller
{
    // GET: /Quiz/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var quiz = await db.Quizzes
            .Include(q => q.Course)
            .Include(q => q.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz is null) return NotFound();

        var userId = User.FindFirst("sub")?.Value ?? string.Empty;
        var attemptCount = await db.QuizAttempts
            .CountAsync(a => a.QuizId == id && a.UserId == userId);

        ViewBag.AttemptCount = attemptCount;
        ViewBag.CanAttempt = attemptCount < quiz.MaxAttempts;
        return View(quiz);
    }

    // POST: /Quiz/Start/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id)
    {
        var quiz = await db.Quizzes.FindAsync(id);
        if (quiz is null) return NotFound();

        var userId = User.FindFirst("sub")?.Value ?? string.Empty;
        var attemptCount = await db.QuizAttempts.CountAsync(a => a.QuizId == id && a.UserId == userId);

        if (attemptCount >= quiz.MaxAttempts)
        {
            TempData["Error"] = "Batas percobaan sudah habis.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var attempt = new QuizAttempt
        {
            QuizId = id,
            UserId = userId,
            UserName = User.Identity?.Name ?? "Unknown",
            AttemptNumber = attemptCount + 1,
            MaxScore = await db.Questions.Where(q => q.QuizId == id).SumAsync(q => q.Points)
        };

        db.QuizAttempts.Add(attempt);
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Take), new { attemptId = attempt.Id });
    }

    // GET: /Quiz/Take/5
    public async Task<IActionResult> Take(int attemptId)
    {
        var attempt = await db.QuizAttempts
            .Include(a => a.Quiz).ThenInclude(q => q.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt is null) return NotFound();

        var userId = User.FindFirst("sub")?.Value;
        if (attempt.UserId != userId) return Forbid();

        if (attempt.Status != AttemptStatus.InProgress)
            return RedirectToAction(nameof(Result), new { attemptId });

        return View(attempt);
    }

    // POST: /Quiz/Submit
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int attemptId, Dictionary<int, int> answers, Dictionary<int, string> essays)
    {
        var attempt = await db.QuizAttempts
            .Include(a => a.Quiz).ThenInclude(q => q.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt is null) return NotFound();

        var userId = User.FindFirst("sub")?.Value;
        if (attempt.UserId != userId) return Forbid();

        int totalScore = 0;

        foreach (var question in attempt.Quiz.Questions)
        {
            var answer = new AttemptAnswer { AttemptId = attemptId, QuestionId = question.Id };

            if (question.Type == QuestionType.Essay)
            {
                answer.EssayAnswer = essays.GetValueOrDefault(question.Id);
            }
            else
            {
                if (answers.TryGetValue(question.Id, out int optionId))
                {
                    answer.SelectedOptionId = optionId;
                    var correct = question.Options.FirstOrDefault(o => o.Id == optionId)?.IsCorrect ?? false;
                    answer.IsCorrect = correct;
                    if (correct) totalScore += question.Points;
                }
            }

            db.AttemptAnswers.Add(answer);
        }

        attempt.Score = totalScore;
        attempt.SubmittedAt = DateTime.UtcNow;
        attempt.IsPassed = totalScore >= (attempt.MaxScore * attempt.Quiz.PassScore / 100);
        attempt.Status = AttemptStatus.Submitted;

        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Result), new { attemptId });
    }

    // GET: /Quiz/Result/5
    public async Task<IActionResult> Result(int attemptId)
    {
        var attempt = await db.QuizAttempts
            .Include(a => a.Quiz).ThenInclude(q => q.Course)
            .Include(a => a.Answers).ThenInclude(a => a.Question).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt is null) return NotFound();

        var userId = User.FindFirst("sub")?.Value;
        if (attempt.UserId != userId && !User.IsInRole("instructor") && !User.IsInRole("admin"))
            return Forbid();

        return View(attempt);
    }

    // POST: /Quiz/GradeEssay  — instruktur nilai jawaban essay
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> GradeEssay(int answerId, int score)
    {
        var answer = await db.AttemptAnswers
            .Include(a => a.Attempt).ThenInclude(a => a.Quiz)
            .Include(a => a.Question)
            .FirstOrDefaultAsync(a => a.Id == answerId);

        if (answer is null) return NotFound();

        score = Math.Clamp(score, 0, answer.Question.Points);
        answer.ManualScore = score;
        answer.IsCorrect = score > 0;

        // Recalculate attempt score
        var attempt = answer.Attempt;
        var allAnswers = await db.AttemptAnswers.Where(a => a.AttemptId == attempt.Id).ToListAsync();
        attempt.Score = allAnswers.Sum(a =>
            a.Question?.Type == QuestionType.Essay ? (a.ManualScore ?? 0) : (a.IsCorrect ? a.Question!.Points : 0));
        attempt.IsPassed = attempt.Score >= (attempt.MaxScore * attempt.Quiz.PassScore / 100);
        attempt.Status = AttemptStatus.Graded;

        await db.SaveChangesAsync();
        TempData["Success"] = "Nilai essay disimpan.";
        return RedirectToAction(nameof(Result), new { attemptId = attempt.Id });
    }

    // GET: /Quiz/Manage/{courseId}  (instructor)
    [Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> Manage(int courseId)
    {
        var course = await db.Courses
            .Include(c => c.Quizzes)
                .ThenInclude(q => q.Questions)
            .Include(c => c.Quizzes)
                .ThenInclude(q => q.Attempts)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course is null) return NotFound();
        return View(course);
    }

    // GET: /Quiz/Create?courseId=5  (instructor)
    [Authorize(Roles = "instructor,admin")]
    public IActionResult Create(int courseId)
    {
        ViewBag.CourseId = courseId;
        return View(new QuizCreateViewModel
        {
            CourseId = courseId,
            TimeLimitMinutes = 30,
            MaxAttempts = 1,
            PassScore = 60
        });
    }

    // POST: /Quiz/Create
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> Create(QuizCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.CourseId = vm.CourseId;
            return View(vm);
        }

        var quiz = new Quiz
        {
            CourseId = vm.CourseId,
            Title = vm.Title,
            Description = vm.Description,
            TimeLimitMinutes = vm.TimeLimitMinutes,
            MaxAttempts = vm.MaxAttempts,
            PassScore = vm.PassScore,
            DueDate = vm.DueDate,
            IsPublished = vm.IsPublished
        };

        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync();

        // Add calendar event
        if (vm.DueDate.HasValue)
        {
            db.CalendarEvents.Add(new CalendarEvent
            {
                CourseId = vm.CourseId,
                Title = $"Quiz: {vm.Title}",
                EventDate = vm.DueDate.Value,
                Type = CalendarEventType.Quiz
            });
            await db.SaveChangesAsync();
        }

        TempData["Success"] = "Quiz berhasil dibuat! Sekarang tambahkan soal.";
        return RedirectToAction("Manage", "Question", new { quizId = quiz.Id });
    }

    // GET: /Quiz/Edit/{id}  (instructor)
    [Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var quiz = await db.Quizzes.Include(q => q.Course).FirstOrDefaultAsync(q => q.Id == id);
        if (quiz is null) return NotFound();

        ViewBag.Quiz = quiz;
        return View(new QuizCreateViewModel
        {
            CourseId = quiz.CourseId,
            Title = quiz.Title,
            Description = quiz.Description,
            TimeLimitMinutes = quiz.TimeLimitMinutes,
            MaxAttempts = quiz.MaxAttempts,
            PassScore = quiz.PassScore,
            DueDate = quiz.DueDate,
            IsPublished = quiz.IsPublished
        });
    }

    // POST: /Quiz/Edit/{id}
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> Edit(int id, QuizCreateViewModel vm)
    {
        var quiz = await db.Quizzes.Include(q => q.Course).FirstOrDefaultAsync(q => q.Id == id);
        if (quiz is null) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.Quiz = quiz;
            return View(vm);
        }

        quiz.Title = vm.Title;
        quiz.Description = vm.Description;
        quiz.TimeLimitMinutes = vm.TimeLimitMinutes;
        quiz.MaxAttempts = vm.MaxAttempts;
        quiz.PassScore = vm.PassScore;
        quiz.DueDate = vm.DueDate;
        quiz.IsPublished = vm.IsPublished;
        await db.SaveChangesAsync();

        TempData["Success"] = "Quiz berhasil diperbarui.";
        return RedirectToAction(nameof(Manage), new { courseId = quiz.CourseId });
    }

    // POST: /Quiz/Delete
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var quiz = await db.Quizzes.FindAsync(id);
        if (quiz is null) return NotFound();

        var courseId = quiz.CourseId;
        db.Quizzes.Remove(quiz);
        await db.SaveChangesAsync();

        TempData["Success"] = "Quiz berhasil dihapus.";
        return RedirectToAction(nameof(Manage), new { courseId });
    }

    // POST: /Quiz/TogglePublish
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> TogglePublish(int id)
    {
        var quiz = await db.Quizzes.FindAsync(id);
        if (quiz is null) return NotFound();

        quiz.IsPublished = !quiz.IsPublished;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Quiz '{quiz.Title}' {(quiz.IsPublished ? "dipublikasikan" : "disembunyikan")}.";
        return RedirectToAction(nameof(Manage), new { courseId = quiz.CourseId });
    }
}
