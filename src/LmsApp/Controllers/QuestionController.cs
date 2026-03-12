using LmsApp.Data;
using LmsApp.Models;
using LmsApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[Authorize(Roles = "instructor,admin")]
public class QuestionController(LmsDbContext db) : Controller
{
    // GET: /Question/Manage/quizId
    public async Task<IActionResult> Manage(int quizId)
    {
        var quiz = await db.Quizzes
            .Include(q => q.Questions).ThenInclude(q => q.Options)
            .Include(q => q.Course)
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz is null) return NotFound();

        return View(quiz);
    }

    // GET: /Question/Create?quizId=5
    public async Task<IActionResult> Create(int quizId)
    {
        var quiz = await db.Quizzes.Include(q => q.Course).FirstOrDefaultAsync(q => q.Id == quizId);
        if (quiz is null) return NotFound();

        ViewBag.Quiz = quiz;
        return View(new QuestionCreateViewModel { QuizId = quizId });
    }

    // POST: /Question/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(QuestionCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Quiz = await db.Quizzes.FindAsync(vm.QuizId);
            return View(vm);
        }

        var order = await db.Questions.CountAsync(q => q.QuizId == vm.QuizId) + 1;

        var question = new Question
        {
            QuizId = vm.QuizId,
            Text = vm.Text,
            Type = vm.Type,
            Points = vm.Points,
            Order = order
        };

        db.Questions.Add(question);
        await db.SaveChangesAsync();

        // Add options for MCQ / TrueFalse
        if (vm.Type == QuestionType.MultipleChoice && vm.Options is not null)
        {
            foreach (var (text, isCorrect) in vm.Options
                .Where(o => !string.IsNullOrWhiteSpace(o.Text))
                .Select(o => (o.Text, o.IsCorrect)))
            {
                db.QuestionOptions.Add(new QuestionOption
                {
                    QuestionId = question.Id,
                    Text = text,
                    IsCorrect = isCorrect
                });
            }
        }
        else if (vm.Type == QuestionType.TrueFalse)
        {
            db.QuestionOptions.AddRange([
                new QuestionOption { QuestionId = question.Id, Text = "Benar", IsCorrect = vm.CorrectTrueFalse == true },
                new QuestionOption { QuestionId = question.Id, Text = "Salah", IsCorrect = vm.CorrectTrueFalse == false }
            ]);
        }

        await db.SaveChangesAsync();
        TempData["Success"] = "Soal berhasil ditambahkan!";
        return RedirectToAction(nameof(Manage), new { quizId = vm.QuizId });
    }

    // POST: /Question/Delete/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var question = await db.Questions.FindAsync(id);
        if (question is null) return NotFound();

        var quizId = question.QuizId;
        db.Questions.Remove(question);
        await db.SaveChangesAsync();

        TempData["Success"] = "Soal dihapus.";
        return RedirectToAction(nameof(Manage), new { quizId });
    }

    // POST: /Question/Reorder  — drag & drop order update via AJAX
    [HttpPost]
    public async Task<IActionResult> Reorder([FromBody] List<int> orderedIds)
    {
        for (int i = 0; i < orderedIds.Count; i++)
        {
            var q = await db.Questions.FindAsync(orderedIds[i]);
            if (q is not null) q.Order = i + 1;
        }
        await db.SaveChangesAsync();
        return Ok();
    }
}
