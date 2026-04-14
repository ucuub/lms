using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[ApiController]
[Route("api/exams")]
[Authorize]
public class ExamsController(LmsDbContext db) : ControllerBase
{
    private string UserId   => User.FindFirst("sub")?.Value  ?? string.Empty;
    private string UserName => User.FindFirst("name")?.Value ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";
    private bool IsAdmin => UserRole == "admin";

    // ── Exam CRUD (admin only) ────────────────────────────────────────────────

    // GET /api/exams — semua user lihat yang published; admin lihat semua
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExamSummaryResponse>>> GetAll()
    {
        var query = db.Exams
            .Include(e => e.Questions)
            .Include(e => e.Attempts)
            .AsQueryable();

        if (!IsAdmin)
            query = query.Where(e => e.IsPublished);

        var exams = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
        return Ok(exams.Select(ToSummary));
    }

    // GET /api/exams/{id} — detail ujian (admin lihat soal + kunci jawaban)
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExamDetailResponse>> GetById(int id)
    {
        var exam = await db.Exams
            .Include(e => e.Questions).ThenInclude(q => q.Options)
            .Include(e => e.Attempts)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exam == null) return NotFound();
        if (!IsAdmin && !exam.IsPublished) return NotFound();

        return Ok(ToDetail(exam, showCorrectAnswers: IsAdmin));
    }

    // POST /api/exams
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ExamSummaryResponse>> Create(ExamRequest req)
    {
        var exam = new Exam
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
        db.Exams.Add(exam);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = exam.Id }, ToSummary(exam));
    }

    // PUT /api/exams/{id}
    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ExamSummaryResponse>> Update(int id, ExamRequest req)
    {
        var exam = await db.Exams
            .Include(e => e.Questions).Include(e => e.Attempts)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (exam == null) return NotFound();

        exam.Title = req.Title;
        exam.Description = req.Description;
        exam.TimeLimitMinutes = req.TimeLimitMinutes;
        exam.MaxAttempts = req.MaxAttempts;
        exam.PassScore = req.PassScore;
        exam.IsPublished = req.IsPublished;
        await db.SaveChangesAsync();
        return Ok(ToSummary(exam));
    }

    // DELETE /api/exams/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var exam = await db.Exams.FindAsync(id);
        if (exam == null) return NotFound();
        db.Exams.Remove(exam);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Question Management (admin only) ──────────────────────────────────────

    // POST /api/exams/{id}/questions
    [HttpPost("{id:int}/questions")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ExamQuestionResponse>> AddQuestion(int id, ExamQuestionRequest req)
    {
        var exam = await db.Exams.FindAsync(id);
        if (exam == null) return NotFound();

        var question = new ExamQuestion
        {
            ExamId = id,
            Text = req.Text,
            Type = req.Type,
            Points = req.Points,
            Order = req.Order
        };

        if (req.Options != null && req.Type != QuestionType.Essay)
        {
            question.Options = req.Options.Select(o => new ExamQuestionOption
            {
                Text = o.Text,
                IsCorrect = o.IsCorrect
            }).ToList();
        }

        db.ExamQuestions.Add(question);
        await db.SaveChangesAsync();
        return Ok(ToQuestionResponse(question));
    }

    // PUT /api/exam-questions/{id}
    [HttpPut("/api/exam-questions/{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ExamQuestionResponse>> UpdateQuestion(int id, ExamQuestionRequest req)
    {
        var question = await db.ExamQuestions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (question == null) return NotFound();

        question.Text = req.Text;
        question.Type = req.Type;
        question.Points = req.Points;
        question.Order = req.Order;

        db.ExamQuestionOptions.RemoveRange(question.Options);
        if (req.Options != null && req.Type != QuestionType.Essay)
        {
            question.Options = req.Options.Select(o => new ExamQuestionOption
            {
                Text = o.Text,
                IsCorrect = o.IsCorrect
            }).ToList();
        }
        else
        {
            question.Options = [];
        }

        await db.SaveChangesAsync();
        return Ok(ToQuestionResponse(question));
    }

    // DELETE /api/exam-questions/{id}
    [HttpDelete("/api/exam-questions/{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        var question = await db.ExamQuestions.FindAsync(id);
        if (question == null) return NotFound();
        db.ExamQuestions.Remove(question);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Attempt (user) ────────────────────────────────────────────────────────

    // POST /api/exams/{id}/start
    [HttpPost("{id:int}/start")]
    public async Task<ActionResult<ExamTakeResponse>> Start(int id)
    {
        var exam = await db.Exams
            .Include(e => e.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exam == null || !exam.IsPublished) return NotFound();

        var attemptCount = await db.ExamAttempts
            .CountAsync(a => a.ExamId == id && a.UserId == UserId);
        if (attemptCount >= exam.MaxAttempts)
            return BadRequest(new { message = $"Maksimal {exam.MaxAttempts} percobaan." });

        var attempt = new ExamAttempt
        {
            ExamId = id,
            UserId = UserId,
            UserName = UserName,
            StartedAt = DateTime.UtcNow
        };
        db.ExamAttempts.Add(attempt);
        await db.SaveChangesAsync();

        var questions = exam.Questions.OrderBy(q => q.Order).Select(q => new ExamTakeQuestionDto(
            q.Id, q.Text, q.Type, q.Points, q.Order,
            q.Options.Select(o => new ExamTakeOptionDto(o.Id, o.Text)).ToList()
        )).ToList();

        return Ok(new ExamTakeResponse(
            attempt.Id, exam.Id, exam.Title, exam.TimeLimitMinutes, attempt.StartedAt, questions));
    }

    // POST /api/exam-attempts/{id}/submit
    [HttpPost("/api/exam-attempts/{id:int}/submit")]
    public async Task<ActionResult<ExamResultResponse>> Submit(int id, SubmitExamRequest req)
    {
        var attempt = await db.ExamAttempts
            .Include(a => a.Exam).ThenInclude(e => e.Questions).ThenInclude(q => q.Options)
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attempt == null || attempt.UserId != UserId) return NotFound();
        if (attempt.SubmittedAt != null) return BadRequest(new { message = "Ujian sudah dikumpulkan." });

        // Cek batas waktu
        if (attempt.Exam.TimeLimitMinutes > 0)
        {
            var elapsed = (DateTime.UtcNow - attempt.StartedAt).TotalMinutes;
            if (elapsed > attempt.Exam.TimeLimitMinutes + 2)
                return BadRequest(new { message = "Waktu pengerjaan sudah habis." });
        }

        int totalScore = 0;
        int maxScore = attempt.Exam.Questions.Sum(q => q.Points);
        var answerResults = new List<ExamAnswerResultDto>();

        foreach (var question in attempt.Exam.Questions)
        {
            var submitted = req.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
            ExamQuestionOption? selectedOption = null;
            string? correctAnswer = null;
            int? earnedPoints = null;
            bool? isCorrect = null;
            bool needsGrading = false;

            if (question.Type == QuestionType.Essay)
            {
                needsGrading = true;
                db.ExamAnswers.Add(new ExamAnswer
                {
                    AttemptId = id,
                    QuestionId = question.Id,
                    EssayAnswer = submitted?.EssayAnswer,
                    IsCorrect = null
                });
            }
            else
            {
                var correctOpt = question.Options.FirstOrDefault(o => o.IsCorrect);
                correctAnswer = correctOpt?.Text;

                if (submitted?.SelectedOptionId != null)
                {
                    selectedOption = question.Options.FirstOrDefault(o => o.Id == submitted.SelectedOptionId);
                    isCorrect = selectedOption?.IsCorrect ?? false;
                    earnedPoints = isCorrect == true ? question.Points : 0;
                    if (isCorrect == true) totalScore += question.Points;
                }
                else
                {
                    isCorrect = false;
                    earnedPoints = 0;
                }

                db.ExamAnswers.Add(new ExamAnswer
                {
                    AttemptId = id,
                    QuestionId = question.Id,
                    SelectedOptionId = submitted?.SelectedOptionId,
                    IsCorrect = isCorrect,
                    EarnedPoints = earnedPoints
                });
            }

            answerResults.Add(new ExamAnswerResultDto(
                0, question.Id, question.Text, question.Type, question.Points,
                earnedPoints, selectedOption?.Text, correctAnswer,
                submitted?.EssayAnswer, isCorrect, needsGrading, null));
        }

        attempt.Score = totalScore;
        attempt.MaxScore = maxScore;
        attempt.IsPassed = maxScore > 0 && (double)totalScore / maxScore * 100 >= attempt.Exam.PassScore;
        attempt.SubmittedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new ExamResultResponse(
            attempt.Id, attempt.Exam.Id, attempt.Exam.Title,
            totalScore, maxScore,
            maxScore > 0 ? Math.Round((double)totalScore / maxScore * 100, 1) : 0,
            attempt.IsPassed, attempt.Exam.PassScore,
            attempt.StartedAt, attempt.SubmittedAt,
            answerResults));
    }

    // GET /api/exam-attempts/{id}/result
    [HttpGet("/api/exam-attempts/{id:int}/result")]
    public async Task<ActionResult<ExamResultResponse>> GetResult(int id)
    {
        var attempt = await db.ExamAttempts
            .Include(a => a.Exam)
            .Include(a => a.Answers).ThenInclude(ans => ans.Question).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attempt == null) return NotFound();
        if (!IsAdmin && attempt.UserId != UserId) return Forbid();

        var answers = attempt.Answers.Select(ans => new ExamAnswerResultDto(
            ans.Id, ans.QuestionId, ans.Question.Text, ans.Question.Type, ans.Question.Points,
            ans.EarnedPoints,
            ans.SelectedOptionId != null ? ans.Question.Options.FirstOrDefault(o => o.Id == ans.SelectedOptionId)?.Text : null,
            ans.Question.Options.FirstOrDefault(o => o.IsCorrect)?.Text,
            ans.EssayAnswer, ans.IsCorrect,
            ans.Question.Type == QuestionType.Essay && ans.IsCorrect == null,
            ans.Feedback
        )).ToList();

        return Ok(new ExamResultResponse(
            attempt.Id, attempt.Exam.Id, attempt.Exam.Title,
            attempt.Score, attempt.MaxScore,
            attempt.MaxScore > 0 ? Math.Round((double)attempt.Score / attempt.MaxScore * 100, 1) : 0,
            attempt.IsPassed, attempt.Exam.PassScore,
            attempt.StartedAt, attempt.SubmittedAt, answers));
    }

    // ── Admin: Bulk Grading ───────────────────────────────────────────────────

    // GET /api/exams/{id}/attempts — semua attempt (admin)
    [HttpGet("{id:int}/attempts")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<IEnumerable<ExamAttemptSummary>>> GetAttempts(int id)
    {
        var attempts = await db.ExamAttempts
            .Include(a => a.Answers).ThenInclude(ans => ans.Question)
            .Where(a => a.ExamId == id && a.SubmittedAt != null)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();

        return Ok(attempts.Select(a => new ExamAttemptSummary(
            a.Id, a.UserId, a.UserName, a.StartedAt, a.SubmittedAt,
            a.Score, a.MaxScore,
            a.MaxScore > 0 ? Math.Round((double)a.Score / a.MaxScore * 100, 1) : 0,
            a.IsPassed,
            a.Answers.Any(ans => ans.Question.Type == QuestionType.Essay && ans.IsCorrect == null)
        )));
    }

    // GET /api/exam-attempts/{id}/detail — detail satu attempt (admin)
    [HttpGet("/api/exam-attempts/{id:int}/detail")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ExamAttemptDetail>> GetAttemptDetail(int id)
    {
        var attempt = await db.ExamAttempts
            .Include(a => a.Exam)
            .Include(a => a.Answers).ThenInclude(ans => ans.Question).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attempt == null) return NotFound();

        var answers = attempt.Answers.Select(ans => new ExamAnswerResultDto(
            ans.Id, ans.QuestionId, ans.Question.Text, ans.Question.Type, ans.Question.Points,
            ans.EarnedPoints,
            ans.SelectedOptionId != null ? ans.Question.Options.FirstOrDefault(o => o.Id == ans.SelectedOptionId)?.Text : null,
            ans.Question.Options.FirstOrDefault(o => o.IsCorrect)?.Text,
            ans.EssayAnswer, ans.IsCorrect,
            ans.Question.Type == QuestionType.Essay && ans.IsCorrect == null,
            ans.Feedback
        )).ToList();

        return Ok(new ExamAttemptDetail(
            attempt.Id, attempt.UserId, attempt.UserName,
            attempt.StartedAt, attempt.SubmittedAt,
            attempt.Score, attempt.MaxScore,
            attempt.MaxScore > 0 ? Math.Round((double)attempt.Score / attempt.MaxScore * 100, 1) : 0,
            attempt.IsPassed, answers));
    }

    // POST /api/exam-answers/{id}/grade — nilai essay satu jawaban
    [HttpPost("/api/exam-answers/{id:int}/grade")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GradeEssay(int id, GradeExamEssayRequest req)
    {
        var answer = await db.ExamAnswers
            .Include(a => a.Attempt).ThenInclude(at => at.Exam).ThenInclude(e => e.Questions)
            .Include(a => a.Question)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (answer == null) return NotFound();
        if (answer.Question.Type != QuestionType.Essay)
            return BadRequest(new { message = "Hanya soal essay yang bisa dinilai manual." });

        answer.EarnedPoints = req.Points;
        answer.IsCorrect = req.Points > 0;
        answer.Feedback = req.Feedback;

        // Hitung ulang total score attempt
        var allAnswers = await db.ExamAnswers.Where(a => a.AttemptId == answer.AttemptId).ToListAsync();
        answer.Attempt.Score = allAnswers.Sum(a => a.EarnedPoints ?? 0);
        if (answer.Attempt.MaxScore > 0)
            answer.Attempt.IsPassed = (double)answer.Attempt.Score / answer.Attempt.MaxScore * 100 >= answer.Attempt.Exam.PassScore;

        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ExamSummaryResponse ToSummary(Exam e) => new(
        e.Id, e.Title, e.Description,
        e.TimeLimitMinutes, e.MaxAttempts, e.PassScore,
        e.IsPublished, e.CreatedByName,
        e.Questions?.Count ?? 0,
        e.Questions?.Sum(q => q.Points) ?? 0,
        e.Attempts?.Count ?? 0,
        e.CreatedAt);

    private static ExamDetailResponse ToDetail(Exam e, bool showCorrectAnswers) => new(
        e.Id, e.Title, e.Description,
        e.TimeLimitMinutes, e.MaxAttempts, e.PassScore,
        e.IsPublished, e.CreatedBy, e.CreatedByName,
        e.Questions?.Count ?? 0,
        e.Questions?.Sum(q => q.Points) ?? 0,
        e.CreatedAt,
        e.Questions?.OrderBy(q => q.Order).Select(q => new ExamQuestionResponse(
            q.Id, q.ExamId, q.Text, q.Type, q.Points, q.Order,
            q.Options.Select(o => new ExamOptionResponse(
                o.Id, o.Text, showCorrectAnswers && o.IsCorrect)).ToList()
        )) ?? []);

    private static ExamQuestionResponse ToQuestionResponse(ExamQuestion q) => new(
        q.Id, q.ExamId, q.Text, q.Type, q.Points, q.Order,
        q.Options.Select(o => new ExamOptionResponse(o.Id, o.Text, o.IsCorrect)).ToList());
}
