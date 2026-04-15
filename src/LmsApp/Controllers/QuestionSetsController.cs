using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[ApiController]
[Route("api/question-sets")]
[Authorize]
public class QuestionSetsController(LmsDbContext db) : ControllerBase
{
    private string UserId   => User.FindFirst("sub")?.Value  ?? string.Empty;
    private string UserName => User.FindFirst("name")?.Value ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";
    private bool IsTeacher => UserRole == "teacher" || UserRole == "admin";
    private bool IsAdmin   => UserRole == "admin";

    // ── QuestionSet CRUD ──────────────────────────────────────────────────────

    // GET /api/question-sets — student lihat yang published; teacher/admin lihat semua miliknya + semua jika admin
    [HttpGet]
    public async Task<ActionResult<IEnumerable<QuestionSetSummaryResponse>>> GetAll()
    {
        var query = db.QuestionSets
            .Include(qs => qs.Questions)
            .Include(qs => qs.Attempts)
            .AsQueryable();

        if (!IsTeacher)
            query = query.Where(qs => qs.IsPublished);
        else if (!IsAdmin)
            query = query.Where(qs => qs.IsPublished || qs.CreatedBy == UserId);

        var sets = await query.OrderByDescending(qs => qs.CreatedAt).ToListAsync();

        var myAttemptCounts = await db.QuestionSetAttempts
            .Where(a => a.UserId == UserId)
            .GroupBy(a => a.QuestionSetId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        return Ok(sets.Select(qs => ToSummary(qs, myAttemptCounts.GetValueOrDefault(qs.Id))));
    }

    // GET /api/question-sets/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuestionSetDetailResponse>> GetById(int id)
    {
        var qs = await db.QuestionSets
            .Include(q => q.Questions).ThenInclude(q => q.Options)
            .Include(q => q.Attempts)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (qs == null) return NotFound();

        var canManage = IsAdmin || (IsTeacher && qs.CreatedBy == UserId);
        if (!canManage && !qs.IsPublished) return NotFound();

        return Ok(ToDetail(qs, showCorrectAnswers: canManage));
    }

    // POST /api/question-sets — teacher/admin buat
    [HttpPost]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<QuestionSetSummaryResponse>> Create(QuestionSetRequest req)
    {
        var qs = new QuestionSet
        {
            Title = req.Title,
            Description = req.Description,
            TimeLimitMinutes = req.TimeLimitMinutes,
            MaxAttempts = req.MaxAttempts,
            PassScore = req.PassScore,
            IsPublished = req.IsPublished,
            CreatedBy = UserId,
            CreatedByName = UserName
        };
        db.QuestionSets.Add(qs);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = qs.Id }, ToSummary(qs, 0));
    }

    // PUT /api/question-sets/{id}
    [HttpPut("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<QuestionSetSummaryResponse>> Update(int id, QuestionSetRequest req)
    {
        var qs = await db.QuestionSets
            .Include(q => q.Questions)
            .Include(q => q.Attempts)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (qs == null) return NotFound();
        if (!IsAdmin && qs.CreatedBy != UserId) return Forbid();

        qs.Title = req.Title;
        qs.Description = req.Description;
        qs.TimeLimitMinutes = req.TimeLimitMinutes;
        qs.MaxAttempts = req.MaxAttempts;
        qs.PassScore = req.PassScore;
        qs.IsPublished = req.IsPublished;

        await db.SaveChangesAsync();
        return Ok(ToSummary(qs, null));
    }

    // DELETE /api/question-sets/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var qs = await db.QuestionSets.FindAsync(id);
        if (qs == null) return NotFound();
        if (!IsAdmin && qs.CreatedBy != UserId) return Forbid();

        db.QuestionSets.Remove(qs);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Question Management ───────────────────────────────────────────────────

    // POST /api/question-sets/{id}/questions — tambah soal custom
    [HttpPost("{id:int}/questions")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<QuestionSetQuestionResponse>> AddQuestion(int id, QuestionSetQuestionRequest req)
    {
        var qs = await db.QuestionSets.FindAsync(id);
        if (qs == null) return NotFound();
        if (!IsAdmin && qs.CreatedBy != UserId) return Forbid();

        var q = new QuestionSetQuestion
        {
            QuestionSetId = id,
            Text = req.Text,
            Type = req.Type,
            Points = req.Points,
            Order = req.Order
        };

        if (req.Options != null)
            foreach (var opt in req.Options)
                q.Options.Add(new QuestionSetOption { Text = opt.Text, IsCorrect = opt.IsCorrect });

        db.QuestionSetQuestions.Add(q);
        await db.SaveChangesAsync();

        return Ok(ToQuestionResponse(q));
    }

    // POST /api/question-sets/{id}/import-from-bank — import soal dari bank
    [HttpPost("{id:int}/import-from-bank")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<IEnumerable<QuestionSetQuestionResponse>>> ImportFromBank(
        int id, ImportQuestionsFromBankRequest req)
    {
        var qs = await db.QuestionSets.FindAsync(id);
        if (qs == null) return NotFound();
        if (!IsAdmin && qs.CreatedBy != UserId) return Forbid();

        var bankQuestions = await db.QuestionBank
            .Include(q => q.Options)
            .Where(q => req.QuestionBankIds.Contains(q.Id))
            .ToListAsync();

        var existingOrder = await db.QuestionSetQuestions
            .Where(q => q.QuestionSetId == id)
            .MaxAsync(q => (int?)q.Order) ?? 0;

        var added = new List<QuestionSetQuestion>();
        foreach (var bq in bankQuestions)
        {
            var q = new QuestionSetQuestion
            {
                QuestionSetId = id,
                BankQuestionId = bq.Id,
                Text = bq.Text,
                Type = bq.Type,
                Points = bq.Points,
                Order = ++existingOrder
            };
            foreach (var opt in bq.Options)
                q.Options.Add(new QuestionSetOption { Text = opt.Text, IsCorrect = opt.IsCorrect });

            db.QuestionSetQuestions.Add(q);
            added.Add(q);
        }

        await db.SaveChangesAsync();
        return Ok(added.Select(ToQuestionResponse));
    }

    // PUT /api/question-set-questions/{id}
    [HttpPut("/api/question-set-questions/{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<QuestionSetQuestionResponse>> UpdateQuestion(int id, QuestionSetQuestionRequest req)
    {
        var q = await db.QuestionSetQuestions
            .Include(x => x.Options)
            .Include(x => x.QuestionSet)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (q == null) return NotFound();
        if (!IsAdmin && q.QuestionSet.CreatedBy != UserId) return Forbid();

        q.Text = req.Text;
        q.Type = req.Type;
        q.Points = req.Points;
        q.Order = req.Order;

        db.QuestionSetOptions.RemoveRange(q.Options);
        q.Options.Clear();

        if (req.Options != null)
            foreach (var opt in req.Options)
                q.Options.Add(new QuestionSetOption { Text = opt.Text, IsCorrect = opt.IsCorrect });

        await db.SaveChangesAsync();
        return Ok(ToQuestionResponse(q));
    }

    // DELETE /api/question-set-questions/{id}
    [HttpDelete("/api/question-set-questions/{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        var q = await db.QuestionSetQuestions
            .Include(x => x.QuestionSet)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (q == null) return NotFound();
        if (!IsAdmin && q.QuestionSet.CreatedBy != UserId) return Forbid();

        db.QuestionSetQuestions.Remove(q);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Attempt ───────────────────────────────────────────────────────────────

    // POST /api/question-sets/{id}/start
    [HttpPost("{id:int}/start")]
    public async Task<ActionResult<QuestionSetTakeResponse>> Start(int id)
    {
        var qs = await db.QuestionSets
            .Include(q => q.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (qs == null || !qs.IsPublished) return NotFound();

        var attemptCount = await db.QuestionSetAttempts
            .CountAsync(a => a.QuestionSetId == id && a.UserId == UserId);

        if (attemptCount >= qs.MaxAttempts)
            return BadRequest(new { message = "Batas percobaan sudah habis." });

        var attempt = new QuestionSetAttempt
        {
            QuestionSetId = id,
            UserId = UserId,
            UserName = UserName,
            MaxScore = qs.Questions.Sum(q => q.Points)
        };
        db.QuestionSetAttempts.Add(attempt);
        await db.SaveChangesAsync();

        return Ok(new QuestionSetTakeResponse(
            attempt.Id,
            qs.Id,
            qs.Title,
            qs.TimeLimitMinutes,
            attempt.StartedAt,
            qs.Questions.OrderBy(q => q.Order).Select(q => new QuestionSetTakeQuestionDto(
                q.Id, q.Text, q.Type, q.Points, q.Order,
                q.Options.Select(o => new QuestionSetTakeOptionDto(o.Id, o.Text)).ToList()
            )).ToList()
        ));
    }

    // POST /api/question-set-attempts/{id}/submit
    [HttpPost("/api/question-set-attempts/{id:int}/submit")]
    public async Task<ActionResult<QuestionSetResultResponse>> Submit(int id, SubmitQuestionSetRequest req)
    {
        var attempt = await db.QuestionSetAttempts
            .Include(a => a.QuestionSet).ThenInclude(qs => qs.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attempt == null) return NotFound();
        if (attempt.UserId != UserId) return Forbid();
        if (attempt.SubmittedAt.HasValue) return BadRequest(new { message = "Sudah dikumpulkan." });

        int score = 0;
        var answers = new List<QuestionSetAnswer>();

        foreach (var ans in req.Answers)
        {
            var question = attempt.QuestionSet.Questions.FirstOrDefault(q => q.Id == ans.QuestionId);
            if (question == null) continue;

            bool? isCorrect = null;
            int? earned = null;

            if (question.Type == QuestionType.Essay)
            {
                // Essay — perlu dinilai manual
            }
            else
            {
                var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                isCorrect = ans.SelectedOptionId.HasValue && correctOption?.Id == ans.SelectedOptionId;
                earned = isCorrect == true ? question.Points : 0;
                score += earned ?? 0;
            }

            answers.Add(new QuestionSetAnswer
            {
                AttemptId = id,
                QuestionId = ans.QuestionId,
                SelectedOptionId = ans.SelectedOptionId,
                EssayAnswer = ans.EssayAnswer,
                IsCorrect = isCorrect,
                EarnedPoints = earned
            });
        }

        db.QuestionSetAnswers.AddRange(answers);

        attempt.SubmittedAt = DateTime.UtcNow;
        attempt.Score = score;
        attempt.IsPassed = attempt.MaxScore > 0 &&
            (score * 100.0 / attempt.MaxScore) >= attempt.QuestionSet.PassScore;

        await db.SaveChangesAsync();

        return Ok(await BuildResult(attempt.Id));
    }

    // GET /api/question-set-attempts/{id}/result
    [HttpGet("/api/question-set-attempts/{id:int}/result")]
    public async Task<ActionResult<QuestionSetResultResponse>> GetResult(int id)
    {
        var attempt = await db.QuestionSetAttempts
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attempt == null) return NotFound();
        if (attempt.UserId != UserId && !IsTeacher) return Forbid();

        return Ok(await BuildResult(id));
    }

    // GET /api/question-sets/{id}/attempts — untuk teacher/admin grading
    [HttpGet("{id:int}/attempts")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<IEnumerable<QuestionSetAttemptSummary>>> GetAttempts(int id)
    {
        var qs = await db.QuestionSets.FindAsync(id);
        if (qs == null) return NotFound();
        if (!IsAdmin && qs.CreatedBy != UserId) return Forbid();

        var attempts = await db.QuestionSetAttempts
            .Include(a => a.Answers).ThenInclude(ans => ans.Question)
            .Where(a => a.QuestionSetId == id && a.SubmittedAt.HasValue)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();

        return Ok(attempts.Select(a => new QuestionSetAttemptSummary(
            a.Id, a.UserId, a.UserName, a.StartedAt, a.SubmittedAt,
            a.Score, a.MaxScore,
            a.MaxScore > 0 ? Math.Round(a.Score * 100.0 / a.MaxScore, 1) : 0,
            a.IsPassed,
            a.Answers.Any(ans => ans.Question.Type == QuestionType.Essay && ans.EarnedPoints == null)
        )));
    }

    // GET /api/question-sets/{id}/attempts/{attemptId}
    [HttpGet("{id:int}/attempts/{attemptId:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<QuestionSetAttemptDetail>> GetAttemptDetail(int id, int attemptId)
    {
        var qs = await db.QuestionSets.FindAsync(id);
        if (qs == null) return NotFound();
        if (!IsAdmin && qs.CreatedBy != UserId) return Forbid();

        return Ok(await BuildAttemptDetail(attemptId));
    }

    // POST /api/question-set-attempts/{id}/grade-essay/{questionId}
    [HttpPost("/api/question-set-attempts/{id:int}/grade-essay/{questionId:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> GradeEssay(int id, int questionId, GradeQuestionSetEssayRequest req)
    {
        var attempt = await db.QuestionSetAttempts
            .Include(a => a.QuestionSet)
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attempt == null) return NotFound();
        if (!IsAdmin && attempt.QuestionSet.CreatedBy != UserId) return Forbid();

        var answer = attempt.Answers.FirstOrDefault(a => a.QuestionId == questionId);
        if (answer == null) return NotFound();

        answer.EarnedPoints = req.Points;
        answer.Feedback = req.Feedback;
        answer.IsCorrect = req.Points > 0;

        // recalculate total score
        var allAnswers = await db.QuestionSetAnswers
            .Where(a => a.AttemptId == id)
            .ToListAsync();
        attempt.Score = allAnswers.Sum(a => a.EarnedPoints ?? 0);
        attempt.IsPassed = attempt.MaxScore > 0 &&
            (attempt.Score * 100.0 / attempt.MaxScore) >= attempt.QuestionSet.PassScore;

        await db.SaveChangesAsync();
        return Ok(new { message = "Nilai disimpan." });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static QuestionSetSummaryResponse ToSummary(QuestionSet qs, int? myAttemptCount) =>
        new(qs.Id, qs.Title, qs.Description, qs.TimeLimitMinutes, qs.MaxAttempts, qs.PassScore,
            qs.IsPublished, qs.CreatedBy, qs.CreatedByName,
            qs.Questions.Count, qs.Questions.Sum(q => q.Points),
            qs.Attempts.Count, myAttemptCount, qs.CreatedAt);

    private static QuestionSetDetailResponse ToDetail(QuestionSet qs, bool showCorrectAnswers) =>
        new(qs.Id, qs.Title, qs.Description, qs.TimeLimitMinutes, qs.MaxAttempts, qs.PassScore,
            qs.IsPublished, qs.CreatedBy, qs.CreatedByName,
            qs.Questions.Count, qs.Questions.Sum(q => q.Points), qs.CreatedAt,
            qs.Questions.OrderBy(q => q.Order).Select(q => new QuestionSetQuestionResponse(
                q.Id, q.QuestionSetId, q.BankQuestionId, q.Text, q.Type, q.Points, q.Order,
                q.Options.Select(o => new QuestionSetOptionResponse(
                    o.Id, o.Text, showCorrectAnswers && o.IsCorrect)).ToList()
            )));

    private static QuestionSetQuestionResponse ToQuestionResponse(QuestionSetQuestion q) =>
        new(q.Id, q.QuestionSetId, q.BankQuestionId, q.Text, q.Type, q.Points, q.Order,
            q.Options.Select(o => new QuestionSetOptionResponse(o.Id, o.Text, o.IsCorrect)).ToList());

    private async Task<QuestionSetResultResponse> BuildResult(int attemptId)
    {
        var attempt = await db.QuestionSetAttempts
            .Include(a => a.QuestionSet)
            .Include(a => a.Answers).ThenInclude(ans => ans.Question).ThenInclude(q => q.Options)
            .FirstAsync(a => a.Id == attemptId);

        var answerDtos = attempt.Answers.Select(ans =>
        {
            var q = ans.Question;
            var selectedOpt = q.Options.FirstOrDefault(o => o.Id == ans.SelectedOptionId);
            var correctOpt  = q.Options.FirstOrDefault(o => o.IsCorrect);
            return new QuestionSetAnswerResultDto(
                ans.Id, q.Id, q.Text, q.Type, q.Points, ans.EarnedPoints,
                selectedOpt?.Text, correctOpt?.Text,
                ans.EssayAnswer, ans.IsCorrect,
                q.Type == QuestionType.Essay && ans.EarnedPoints == null,
                ans.Feedback
            );
        }).ToList();

        return new QuestionSetResultResponse(
            attempt.Id, attempt.QuestionSetId, attempt.QuestionSet.Title,
            attempt.Score, attempt.MaxScore,
            attempt.MaxScore > 0 ? Math.Round(attempt.Score * 100.0 / attempt.MaxScore, 1) : 0,
            attempt.IsPassed, attempt.QuestionSet.PassScore,
            attempt.StartedAt, attempt.SubmittedAt, answerDtos
        );
    }

    private async Task<QuestionSetAttemptDetail> BuildAttemptDetail(int attemptId)
    {
        var attempt = await db.QuestionSetAttempts
            .Include(a => a.QuestionSet)
            .Include(a => a.Answers).ThenInclude(ans => ans.Question).ThenInclude(q => q.Options)
            .FirstAsync(a => a.Id == attemptId);

        var answerDtos = attempt.Answers.Select(ans =>
        {
            var q = ans.Question;
            var selectedOpt = q.Options.FirstOrDefault(o => o.Id == ans.SelectedOptionId);
            var correctOpt  = q.Options.FirstOrDefault(o => o.IsCorrect);
            return new QuestionSetAnswerResultDto(
                ans.Id, q.Id, q.Text, q.Type, q.Points, ans.EarnedPoints,
                selectedOpt?.Text, correctOpt?.Text,
                ans.EssayAnswer, ans.IsCorrect,
                q.Type == QuestionType.Essay && ans.EarnedPoints == null,
                ans.Feedback
            );
        }).ToList();

        return new QuestionSetAttemptDetail(
            attempt.Id, attempt.UserId, attempt.UserName,
            attempt.StartedAt, attempt.SubmittedAt,
            attempt.Score, attempt.MaxScore,
            attempt.MaxScore > 0 ? Math.Round(attempt.Score * 100.0 / attempt.MaxScore, 1) : 0,
            attempt.IsPassed, answerDtos
        );
    }
}
