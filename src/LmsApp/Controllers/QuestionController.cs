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

    // POST: /Question/ImportCsv
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportCsv(int quizId, IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "File CSV tidak valid.";
            return RedirectToAction(nameof(Create), new { quizId });
        }

        int order = await db.Questions.CountAsync(q => q.QuizId == quizId);
        int imported = 0;
        var errors = new List<string>();

        using var reader = new System.IO.StreamReader(file.OpenReadStream());
        int lineNum = 0;
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            lineNum++;
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#')) continue;

            var cols = line.Split(',');
            if (cols.Length < 3)
            {
                errors.Add($"Baris {lineNum}: Format tidak valid (minimal 3 kolom).");
                continue;
            }

            var tipe = cols[0].Trim().ToLower();
            var text = cols[1].Trim();
            if (!int.TryParse(cols[2].Trim(), out int points)) points = 10;

            order++;
            var question = new Question
            {
                QuizId = quizId,
                Text = text,
                Points = points,
                Order = order
            };

            if (tipe == "mcq")
            {
                question.Type = QuestionType.MultipleChoice;
                db.Questions.Add(question);
                await db.SaveChangesAsync();

                var optTexts = new[] {
                    cols.ElementAtOrDefault(3)?.Trim(),
                    cols.ElementAtOrDefault(4)?.Trim(),
                    cols.ElementAtOrDefault(5)?.Trim(),
                    cols.ElementAtOrDefault(6)?.Trim()
                };
                var correct = cols.ElementAtOrDefault(7)?.Trim().ToUpper() ?? "A";

                for (int i = 0; i < 4; i++)
                {
                    if (string.IsNullOrWhiteSpace(optTexts[i])) continue;
                    db.QuestionOptions.Add(new QuestionOption
                    {
                        QuestionId = question.Id,
                        Text = optTexts[i]!,
                        IsCorrect = correct == ((char)('A' + i)).ToString()
                    });
                }
            }
            else if (tipe == "tf")
            {
                question.Type = QuestionType.TrueFalse;
                var ans = cols.ElementAtOrDefault(7)?.Trim().ToLower() ?? "true";
                db.Questions.Add(question);
                await db.SaveChangesAsync();
                db.QuestionOptions.AddRange([
                    new QuestionOption { QuestionId = question.Id, Text = "Benar", IsCorrect = ans == "true" },
                    new QuestionOption { QuestionId = question.Id, Text = "Salah", IsCorrect = ans == "false" }
                ]);
            }
            else if (tipe == "essay")
            {
                question.Type = QuestionType.Essay;
                db.Questions.Add(question);
            }
            else
            {
                errors.Add($"Baris {lineNum}: Tipe '{tipe}' tidak dikenal (gunakan mcq/tf/essay).");
                order--;
                continue;
            }

            await db.SaveChangesAsync();
            imported++;
        }

        TempData["Success"] = $"{imported} soal berhasil diimport.";
        if (errors.Count > 0)
            TempData["Error"] = string.Join(" | ", errors);

        return RedirectToAction(nameof(Manage), new { quizId });
    }
}
