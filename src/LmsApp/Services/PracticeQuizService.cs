using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Services;

public class PracticeQuizService(LmsDbContext db) : IPracticeQuizService
{
    // ── List ──────────────────────────────────────────────────────────────────

    public async Task<List<PracticeQuizDto>> GetAllAsync(string userId)
    {
        var quizzes = await db.PracticeQuizzes
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        // Ambil jumlah attempt per quiz untuk user ini dalam satu query
        var quizIds = quizzes.Select(q => q.Id).ToList();
        var attemptCounts = await db.PracticeAttempts
            .Where(a => a.UserId == userId && quizIds.Contains(a.PracticeQuizId))
            .GroupBy(a => a.PracticeQuizId)
            .Select(g => new { QuizId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.QuizId, x => x.Count);

        return quizzes.Select(q => new PracticeQuizDto(
            q.Id, q.Title, q.Description,
            q.QuestionCount, q.ShuffleQuestions, q.ShuffleOptions,
            q.TimeLimitMinutes, q.IsActive, q.CreatedByName, q.CreatedAt,
            attemptCounts.GetValueOrDefault(q.Id, 0)
        )).ToList();
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<PracticeQuizDto> CreateAsync(
        CreatePracticeQuizRequest req, string userId, string userName)
    {
        // Pastikan QuestionCount tidak melebihi jumlah soal di bank
        var bankCount = await db.QuestionBank.CountAsync();
        if (bankCount == 0)
            throw new InvalidOperationException(
                "Bank soal masih kosong. Tambahkan soal ke bank soal terlebih dahulu.");

        var questionCount = Math.Min(req.QuestionCount, bankCount);

        var quiz = new PracticeQuiz
        {
            Title           = req.Title.Trim(),
            Description     = req.Description?.Trim(),
            QuestionCount   = questionCount,
            ShuffleQuestions = req.ShuffleQuestions,
            ShuffleOptions  = req.ShuffleOptions,
            TimeLimitMinutes = req.TimeLimitMinutes,
            CreatedBy       = userId,
            CreatedByName   = userName
        };

        db.PracticeQuizzes.Add(quiz);
        await db.SaveChangesAsync();

        return new PracticeQuizDto(
            quiz.Id, quiz.Title, quiz.Description,
            quiz.QuestionCount, quiz.ShuffleQuestions, quiz.ShuffleOptions,
            quiz.TimeLimitMinutes, quiz.IsActive, quiz.CreatedByName, quiz.CreatedAt,
            MyAttemptCount: 0
        );
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    public async Task DeleteAsync(int quizId, string userId)
    {
        var quiz = await db.PracticeQuizzes.FindAsync(quizId)
            ?? throw new KeyNotFoundException("Practice quiz tidak ditemukan.");

        db.PracticeQuizzes.Remove(quiz);
        await db.SaveChangesAsync();
    }

    // ── Start Attempt ─────────────────────────────────────────────────────────

    public async Task<StartPracticeResponse> StartAttemptAsync(
        int quizId, string userId, string userName)
    {
        var quiz = await db.PracticeQuizzes.FindAsync(quizId)
            ?? throw new KeyNotFoundException("Practice quiz tidak ditemukan.");

        if (!quiz.IsActive)
            throw new InvalidOperationException("Practice quiz ini sudah tidak aktif.");

        // Ambil semua soal dari bank, acak, ambil sebanyak QuestionCount
        var bankQuestions = await db.QuestionBank
            .Include(q => q.Options)
            .OrderBy(q => Guid.NewGuid()) // random shuffle di database
            .Take(quiz.QuestionCount)
            .ToListAsync();

        if (bankQuestions.Count == 0)
            throw new InvalidOperationException("Bank soal kosong. Tidak bisa memulai practice quiz.");

        // Buat attempt
        var attempt = new PracticeAttempt
        {
            PracticeQuizId = quiz.Id,
            UserId         = userId,
            UserName       = userName,
            TotalQuestions = bankQuestions.Count
        };
        db.PracticeAttempts.Add(attempt);
        await db.SaveChangesAsync();

        // Buat answer slot (kosong) untuk setiap soal
        var answerSlots = bankQuestions.Select((q, idx) => new PracticeAttemptAnswer
        {
            AttemptId      = attempt.Id,
            BankQuestionId = q.Id,
            DisplayOrder   = idx + 1
        }).ToList();
        db.PracticeAttemptAnswers.AddRange(answerSlots);
        await db.SaveChangesAsync();

        // Build response — acak opsi jika ShuffleOptions = true
        var questions = bankQuestions.Select((q, idx) =>
        {
            var options = q.Options.ToList();
            if (quiz.ShuffleOptions && options.Count > 0)
                options = options.OrderBy(_ => Guid.NewGuid()).ToList();

            return new PracticeQuestionDto(
                q.Id,
                q.Text,
                q.Type.ToString(),
                q.Points,
                DisplayOrder: idx + 1,
                options.Count > 0
                    ? options.Select(o => new PracticeOptionDto(o.Id, o.Text)).ToList()
                    : null
            );
        }).ToList();

        return new StartPracticeResponse(
            attempt.Id, quiz.Id, quiz.Title, quiz.TimeLimitMinutes, questions
        );
    }

    // ── Submit ────────────────────────────────────────────────────────────────

    public async Task<PracticeResultDto> SubmitAttemptAsync(
        int attemptId, string userId, SubmitPracticeRequest req)
    {
        var attempt = await db.PracticeAttempts
            .Include(a => a.Answers)
                .ThenInclude(ans => ans.BankQuestion)
                    .ThenInclude(q => q.Options)
            .Include(a => a.PracticeQuiz)
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId)
            ?? throw new KeyNotFoundException("Attempt tidak ditemukan.");

        if (attempt.SubmittedAt.HasValue)
            throw new InvalidOperationException("Attempt ini sudah pernah di-submit.");

        // Buat lookup: questionBankId → selected option
        var answerMap = req.Answers.ToDictionary(a => a.QuestionId, a => a.SelectedOptionId);

        int correct = 0;
        var details = new List<PracticeResultDetailDto>();

        foreach (var slot in attempt.Answers)
        {
            var question = slot.BankQuestion;
            answerMap.TryGetValue(question.Id, out var selectedOptId);

            // Update jawaban user ke DB
            slot.SelectedOptionId = selectedOptId;

            // Cari opsi yang benar
            var correctOpt = question.Options.FirstOrDefault(o => o.IsCorrect);
            var selectedOpt = selectedOptId.HasValue
                ? question.Options.FirstOrDefault(o => o.Id == selectedOptId.Value)
                : null;

            bool isCorrect = false;
            if (question.Type == QuestionType.Essay)
            {
                // Essay tidak dinilai otomatis — anggap benar jika ada isian
                isCorrect = selectedOptId.HasValue;
            }
            else
            {
                isCorrect = correctOpt != null
                    && selectedOptId.HasValue
                    && selectedOptId.Value == correctOpt.Id;
            }

            if (isCorrect) correct++;

            details.Add(new PracticeResultDetailDto(
                question.Id,
                question.Text,
                question.Type.ToString(),
                isCorrect,
                selectedOptId,
                selectedOpt?.Text,
                correctOpt?.Id,
                correctOpt?.Text
            ));
        }

        var total = attempt.Answers.Count;
        var score = total == 0 ? 0.0 : Math.Round(correct / (double)total * 100, 1);

        attempt.Score          = score;
        attempt.CorrectAnswers = correct;
        attempt.SubmittedAt    = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return new PracticeResultDto(
            attempt.Id,
            attempt.PracticeQuizId,
            attempt.PracticeQuiz.Title,
            score,
            total,
            correct,
            attempt.StartedAt,
            attempt.SubmittedAt!.Value,
            details
        );
    }

    // ── Get Result ────────────────────────────────────────────────────────────

    public async Task<PracticeResultDto> GetResultAsync(int attemptId, string userId)
    {
        var attempt = await db.PracticeAttempts
            .Include(a => a.Answers)
                .ThenInclude(ans => ans.BankQuestion)
                    .ThenInclude(q => q.Options)
            .Include(a => a.Answers)
                .ThenInclude(ans => ans.SelectedOption)
            .Include(a => a.PracticeQuiz)
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId)
            ?? throw new KeyNotFoundException("Attempt tidak ditemukan.");

        if (!attempt.SubmittedAt.HasValue)
            throw new InvalidOperationException("Attempt belum di-submit.");

        var details = attempt.Answers.Select(slot =>
        {
            var question   = slot.BankQuestion;
            var correctOpt = question.Options.FirstOrDefault(o => o.IsCorrect);

            bool isCorrect;
            if (question.Type == QuestionType.Essay)
                isCorrect = slot.SelectedOptionId.HasValue;
            else
                isCorrect = correctOpt != null
                    && slot.SelectedOptionId.HasValue
                    && slot.SelectedOptionId.Value == correctOpt.Id;

            return new PracticeResultDetailDto(
                question.Id,
                question.Text,
                question.Type.ToString(),
                isCorrect,
                slot.SelectedOptionId,
                slot.SelectedOption?.Text,
                correctOpt?.Id,
                correctOpt?.Text
            );
        }).ToList();

        return new PracticeResultDto(
            attempt.Id,
            attempt.PracticeQuizId,
            attempt.PracticeQuiz.Title,
            attempt.Score ?? 0,
            attempt.TotalQuestions,
            attempt.CorrectAnswers ?? 0,
            attempt.StartedAt,
            attempt.SubmittedAt!.Value,
            details
        );
    }

    // ── My Attempts ───────────────────────────────────────────────────────────

    public async Task<List<PracticeAttemptSummaryDto>> GetMyAttemptsAsync(string userId)
    {
        var attempts = await db.PracticeAttempts
            .Include(a => a.PracticeQuiz)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync();

        return attempts.Select(a => new PracticeAttemptSummaryDto(
            a.Id,
            a.PracticeQuizId,
            a.PracticeQuiz.Title,
            a.StartedAt,
            a.SubmittedAt,
            a.Score,
            a.TotalQuestions,
            a.CorrectAnswers,
            IsCompleted: a.SubmittedAt.HasValue
        )).ToList();
    }
}
