using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class QuizzesController(LmsDbContext db, INotificationService notifService) : ControllerBase
{
    private string UserId => User.FindFirst("sub")?.Value ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";

    // ── Quiz CRUD ─────────────────────────────────────────────────────────────

    [HttpGet("courses/{courseId:int}/quizzes")]
    public async Task<ActionResult<IEnumerable<QuizResponse>>> GetByCourse(int courseId)
    {
        var isTeacher = await IsTeacherOrAdmin(courseId);
        var isEnrolled = await db.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserId == UserId);
        if (!isTeacher && !isEnrolled) return Forbid();

        var quizzes = await db.Quizzes
            .Include(q => q.Questions)
            .Include(q => q.Attempts)
            .Where(q => q.CourseId == courseId && (isTeacher || q.IsPublished))
            .OrderBy(q => q.CreatedAt)
            .ToListAsync();

        return Ok(quizzes.Select(q => ToQuizResponse(q)));
    }

    [HttpGet("quizzes/{id:int}")]
    public async Task<ActionResult<QuizResponse>> GetById(int id)
    {
        var quiz = await db.Quizzes
            .Include(q => q.Questions).ThenInclude(q => q.Options)
            .Include(q => q.Attempts)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (quiz == null) return NotFound();

        var isTeacher = await IsTeacherOrAdmin(quiz.CourseId);
        var isEnrolled = await db.Enrollments.AnyAsync(e => e.CourseId == quiz.CourseId && e.UserId == UserId);
        // Non-enrolled boleh lihat quiz yang published
        if (!isTeacher && !isEnrolled && !quiz.IsPublished) return Forbid();

        return Ok(ToQuizResponse(quiz));
    }

    [HttpPost("courses/{courseId:int}/quizzes")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<QuizResponse>> Create(int courseId, QuizRequest req)
    {
        if (!await IsTeacherOrAdmin(courseId)) return Forbid();

        var quiz = new Quiz
        {
            CourseId = courseId,
            Title = req.Title,
            Description = req.Description,
            TimeLimitMinutes = req.TimeLimitMinutes,
            MaxAttempts = req.MaxAttempts,
            PassScore = req.PassScore,
            DueDate = req.DueDate,
            IsPublished = req.IsPublished,
            ShowAnswers = req.ShowAnswers
        };
        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = quiz.Id }, ToQuizResponse(quiz));
    }

    [HttpPut("quizzes/{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<QuizResponse>> Update(int id, QuizRequest req)
    {
        var quiz = await db.Quizzes
            .Include(q => q.Questions).Include(q => q.Attempts)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (quiz == null) return NotFound();
        if (!await IsTeacherOrAdmin(quiz.CourseId)) return Forbid();

        var wasPublished = quiz.IsPublished;

        quiz.Title            = req.Title;
        quiz.Description      = req.Description;
        quiz.TimeLimitMinutes = req.TimeLimitMinutes;
        quiz.MaxAttempts      = req.MaxAttempts;
        quiz.PassScore        = req.PassScore;
        quiz.DueDate          = req.DueDate;
        quiz.IsPublished      = req.IsPublished;
        quiz.ShowAnswers      = req.ShowAnswers;
        await db.SaveChangesAsync();

        // Kirim notifikasi ke semua student saat quiz baru dipublish
        if (!wasPublished && req.IsPublished)
        {
            var enrolledIds = await db.Enrollments
                .Where(e => e.CourseId == quiz.CourseId && e.Status == EnrollmentStatus.Active)
                .Select(e => e.UserId)
                .ToListAsync();

            await notifService.CreateForQuizAvailableAsync(
                enrolledIds, quiz.Title, quiz.CourseId, quiz.Id);
        }

        return Ok(ToQuizResponse(quiz));
    }

    [HttpDelete("quizzes/{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var quiz = await db.Quizzes.FindAsync(id);
        if (quiz == null) return NotFound();
        if (!await IsTeacherOrAdmin(quiz.CourseId)) return Forbid();

        db.Quizzes.Remove(quiz);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Questions ─────────────────────────────────────────────────────────────

    [HttpGet("quizzes/{quizId:int}/questions")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<IEnumerable<QuestionResponse>>> GetQuestions(int quizId)
    {
        var quiz = await db.Quizzes.FindAsync(quizId);
        if (quiz == null) return NotFound();
        if (!await IsTeacherOrAdmin(quiz.CourseId)) return Forbid();

        var questions = await db.Questions
            .Include(q => q.Options)
            .Where(q => q.QuizId == quizId)
            .OrderBy(q => q.Order)
            .ToListAsync();

        return Ok(questions.Select(ToQuestionResponse));
    }

    [HttpPost("quizzes/{quizId:int}/questions")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<QuestionResponse>> AddQuestion(int quizId, QuestionRequest req)
    {
        var quiz = await db.Quizzes.FindAsync(quizId);
        if (quiz == null) return NotFound();
        if (!await IsTeacherOrAdmin(quiz.CourseId)) return Forbid();

        var question = new Question
        {
            QuizId = quizId,
            Text = req.Text,
            Type = req.Type,
            Points = req.Points,
            Order = req.Order
        };

        if (req.Options != null && req.Type != QuestionType.Essay)
        {
            question.Options = req.Options.Select(o => new QuestionOption
            {
                Text = o.Text,
                IsCorrect = o.IsCorrect
            }).ToList();
        }

        db.Questions.Add(question);
        await db.SaveChangesAsync();
        return Ok(ToQuestionResponse(question));
    }

    [HttpPut("questions/{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<QuestionResponse>> UpdateQuestion(int id, QuestionRequest req)
    {
        var question = await db.Questions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (question == null) return NotFound();

        var quiz = await db.Quizzes.FindAsync(question.QuizId);
        if (!await IsTeacherOrAdmin(quiz!.CourseId)) return Forbid();

        question.Text = req.Text;
        question.Type = req.Type;
        question.Points = req.Points;
        question.Order = req.Order;

        if (req.Options != null)
        {
            db.QuestionOptions.RemoveRange(question.Options);
            question.Options = req.Options.Select(o => new QuestionOption
            {
                Text = o.Text,
                IsCorrect = o.IsCorrect
            }).ToList();
        }

        await db.SaveChangesAsync();
        return Ok(ToQuestionResponse(question));
    }

    [HttpDelete("questions/{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        var question = await db.Questions.FindAsync(id);
        if (question == null) return NotFound();
        var quiz = await db.Quizzes.FindAsync(question.QuizId);
        if (!await IsTeacherOrAdmin(quiz!.CourseId)) return Forbid();

        db.Questions.Remove(question);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // Import from Question Bank
    [HttpPost("quizzes/{quizId:int}/import-from-bank")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> ImportFromBank(int quizId, ImportFromBankRequest req)
    {
        var quiz = await db.Quizzes.FindAsync(quizId);
        if (quiz == null) return NotFound();
        if (!await IsTeacherOrAdmin(quiz.CourseId)) return Forbid();

        var bankItems = await db.QuestionBank
            .Include(q => q.Options)
            .Where(q => req.QuestionBankIds.Contains(q.Id))
            .ToListAsync();

        var maxOrder = await db.Questions.Where(q => q.QuizId == quizId).MaxAsync(q => (int?)q.Order) ?? 0;

        foreach (var bank in bankItems)
        {
            var question = new Question
            {
                QuizId = quizId,
                Text = bank.Text,
                Type = bank.Type,
                Points = bank.Points,
                Order = ++maxOrder,
                Options = bank.Options.Select(o => new QuestionOption
                {
                    Text = o.Text,
                    IsCorrect = o.IsCorrect
                }).ToList()
            };
            db.Questions.Add(question);
        }

        await db.SaveChangesAsync();
        return Ok(new { imported = bankItems.Count });
    }

    // ── Quiz Attempt ──────────────────────────────────────────────────────────

    // GET /api/quizzes/available — semua quiz published, tanpa perlu enrollment
    [HttpGet("quizzes/available")]
    public async Task<IActionResult> GetAvailableQuizzes()
    {
        var quizzes = await db.Quizzes
            .Include(q => q.Questions)
            .Include(q => q.Course)
            .Where(q => q.IsPublished)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        var myAttempts = await db.QuizAttempts
            .Where(a => a.UserId == UserId && a.SubmittedAt != null)
            .GroupBy(a => a.QuizId)
            .Select(g => new {
                QuizId = g.Key,
                Count = g.Count(),
                BestPct = g.Max(a => a.MaxScore > 0 ? (double?)a.Score / a.MaxScore * 100 : null)
            })
            .ToListAsync();

        var result = quizzes.Select(q => {
            var att = myAttempts.FirstOrDefault(a => a.QuizId == q.Id);
            return new {
                q.Id,
                q.Title,
                q.Description,
                q.TimeLimitMinutes,
                q.MaxAttempts,
                q.PassScore,
                CourseId = q.CourseId,
                CourseName = q.Course.Title,
                QuestionCount = q.Questions.Count,
                MyAttempts = att?.Count ?? 0,
                BestScore = att?.BestPct.HasValue == true ? Math.Round(att.BestPct!.Value, 1) : (double?)null,
                CanAttempt = (att?.Count ?? 0) < q.MaxAttempts
            };
        });

        return Ok(result);
    }

    [HttpGet("quizzes/{quizId:int}/in-progress")]
    public async Task<IActionResult> CheckInProgress(int quizId)
    {
        var has = await db.QuizAttempts.AnyAsync(
            a => a.QuizId == quizId && a.UserId == UserId && a.SubmittedAt == null);
        return Ok(new { hasInProgress = has });
    }

    [HttpPost("quizzes/{quizId:int}/start")]
    public async Task<ActionResult<QuizTakeResponse>> StartAttempt(int quizId)
    {
        var quiz = await db.Quizzes
            .Include(q => q.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz == null || !quiz.IsPublished) return NotFound();

        // Cek apakah ada attempt yang belum selesai (resume)
        var inProgress = await db.QuizAttempts
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.QuizId == quizId && a.UserId == UserId && a.SubmittedAt == null);

        if (inProgress != null)
        {
            var savedAnswers = inProgress.Answers.Select(a => new SavedAnswerDto(
                a.QuestionId, a.SelectedOptionId, a.EssayAnswer)).ToList();

            var questions = quiz.Questions.OrderBy(q => q.Order).Select(q => new TakeQuestionDto(
                q.Id, q.Text, q.Type, q.Points, q.Order,
                q.Options.Select(o => new TakeOptionDto(o.Id, o.Text)).ToList()
            )).ToList();

            return Ok(new QuizTakeResponse(
                inProgress.Id, quiz.Id, quiz.Title, quiz.TimeLimitMinutes,
                inProgress.StartedAt, questions, true, savedAnswers));
        }

        // Cek max attempts (hanya attempt yang sudah di-submit)
        var submittedCount = await db.QuizAttempts.CountAsync(
            a => a.QuizId == quizId && a.UserId == UserId && a.SubmittedAt != null);
        if (submittedCount >= quiz.MaxAttempts)
            return BadRequest(new { message = $"Maksimal {quiz.MaxAttempts} percobaan." });

        // Buat attempt baru
        var attempt = new QuizAttempt
        {
            QuizId = quizId,
            UserId = UserId,
            AttemptNumber = submittedCount + 1,
            StartedAt = DateTime.UtcNow
        };
        db.QuizAttempts.Add(attempt);
        await db.SaveChangesAsync();

        var qs = quiz.Questions.OrderBy(q => q.Order).Select(q => new TakeQuestionDto(
            q.Id, q.Text, q.Type, q.Points, q.Order,
            q.Options.Select(o => new TakeOptionDto(o.Id, o.Text)).ToList()
        )).ToList();

        return Ok(new QuizTakeResponse(
            attempt.Id, quiz.Id, quiz.Title, quiz.TimeLimitMinutes,
            attempt.StartedAt, qs, false, []));
    }

    [HttpPost("attempts/{attemptId:int}/save-progress")]
    public async Task<IActionResult> SaveProgress(int attemptId, SaveProgressRequest req)
    {
        var attempt = await db.QuizAttempts
            .Include(a => a.Answers)
            .Include(a => a.Quiz)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt == null || attempt.UserId != UserId) return NotFound();
        if (attempt.SubmittedAt != null) return BadRequest(new { message = "Attempt sudah dikumpulkan." });

        foreach (var ans in req.Answers)
        {
            var existing = attempt.Answers.FirstOrDefault(a => a.QuestionId == ans.QuestionId);
            if (existing != null)
            {
                existing.SelectedOptionId = ans.SelectedOptionId;
                existing.EssayAnswer = ans.EssayAnswer;
            }
            else
            {
                db.AttemptAnswers.Add(new AttemptAnswer
                {
                    AttemptId = attemptId,
                    QuestionId = ans.QuestionId,
                    SelectedOptionId = ans.SelectedOptionId,
                    EssayAnswer = ans.EssayAnswer
                });
            }
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("attempts/{attemptId:int}/submit")]
    public async Task<ActionResult<QuizResultResponse>> Submit(int attemptId, SubmitQuizRequest req)
    {
        var attempt = await db.QuizAttempts
            .Include(a => a.Quiz).ThenInclude(q => q.Questions).ThenInclude(q => q.Options)
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt == null || attempt.UserId != UserId) return NotFound();
        if (attempt.SubmittedAt != null) return BadRequest(new { message = "Attempt sudah dikumpulkan." });

        // Check time limit
        var elapsed = (DateTime.UtcNow - attempt.StartedAt).TotalMinutes;
        if (attempt.Quiz.TimeLimitMinutes > 0 && elapsed > attempt.Quiz.TimeLimitMinutes + 2)
            return BadRequest(new { message = "Waktu pengerjaan sudah habis." });

        int totalScore = 0;
        int maxScore = attempt.Quiz.Questions.Sum(q => q.Points);
        var answerResults = new List<AnswerResultDto>();

        foreach (var question in attempt.Quiz.Questions)
        {
            var submitted = req.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
            QuestionOption? selectedOption = null;
            string? correctAnswer = null;
            int? earnedPoints = null;
            bool? isCorrect = null;
            bool needsGrading = false;

            if (question.Type == QuestionType.Essay)
            {
                needsGrading = true;
                earnedPoints = null;
                db.AttemptAnswers.Add(new AttemptAnswer
                {
                    AttemptId = attemptId,
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

                db.AttemptAnswers.Add(new AttemptAnswer
                {
                    AttemptId = attemptId,
                    QuestionId = question.Id,
                    SelectedOptionId = submitted?.SelectedOptionId,
                    IsCorrect = isCorrect,
                    EarnedPoints = earnedPoints
                });
            }

            var showAnswers = attempt.Quiz.ShowAnswers;
            answerResults.Add(new AnswerResultDto(
                question.Id, question.Text, question.Type, question.Points,
                earnedPoints,
                showAnswers ? selectedOption?.Text : null,
                showAnswers ? correctAnswer : null,
                showAnswers ? submitted?.EssayAnswer : null,
                showAnswers ? isCorrect : null,
                needsGrading));
        }

        attempt.Score = totalScore;
        attempt.MaxScore = maxScore;
        attempt.IsPassed = (double)totalScore / maxScore * 100 >= attempt.Quiz.PassScore;
        attempt.SubmittedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new QuizResultResponse(
            attempt.Id, attempt.Quiz.Id, attempt.Quiz.Title,
            totalScore, maxScore,
            Math.Round((double)totalScore / maxScore * 100, 1),
            attempt.IsPassed, attempt.Quiz.PassScore,
            attempt.StartedAt, attempt.SubmittedAt,
            answerResults, attempt.Quiz.ShowAnswers));
    }

    [HttpGet("attempts/{attemptId:int}/result")]
    public async Task<ActionResult<QuizResultResponse>> GetResult(int attemptId)
    {
        var attempt = await db.QuizAttempts
            .Include(a => a.Quiz)
            .Include(a => a.Answers).ThenInclude(ans => ans.Question).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt == null) return NotFound();

        var isTeacher = await IsTeacherOrAdmin(attempt.Quiz.CourseId);
        if (!isTeacher && attempt.UserId != UserId) return Forbid();

        var showAnswers = attempt.Quiz.ShowAnswers;
        var answers = attempt.Answers.Select(ans => new AnswerResultDto(
            ans.QuestionId, ans.Question.Text, ans.Question.Type, ans.Question.Points,
            ans.EarnedPoints,
            showAnswers && ans.SelectedOptionId != null
                ? ans.Question.Options.FirstOrDefault(o => o.Id == ans.SelectedOptionId)?.Text : null,
            showAnswers ? ans.Question.Options.FirstOrDefault(o => o.IsCorrect)?.Text : null,
            showAnswers ? ans.EssayAnswer : null,
            showAnswers ? ans.IsCorrect : null,
            ans.Question.Type == QuestionType.Essay && ans.IsCorrect == null
        )).ToList();

        return Ok(new QuizResultResponse(
            attempt.Id, attempt.Quiz.Id, attempt.Quiz.Title,
            attempt.Score, attempt.MaxScore,
            attempt.MaxScore > 0 ? Math.Round((double)attempt.Score / attempt.MaxScore * 100, 1) : 0,
            attempt.IsPassed, attempt.Quiz.PassScore,
            attempt.StartedAt, attempt.SubmittedAt, answers, attempt.Quiz.ShowAnswers));
    }

    // Grade essay answer
    [HttpPost("attempt-answers/{answerId:int}/grade")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> GradeEssay(int answerId, [FromBody] GradeEssayRequest req)
    {
        var answer = await db.AttemptAnswers
            .Include(a => a.Attempt).ThenInclude(at => at.Quiz)
            .Include(a => a.Question)
            .FirstOrDefaultAsync(a => a.Id == answerId);

        if (answer == null) return NotFound();
        if (!await IsTeacherOrAdmin(answer.Attempt.Quiz.CourseId)) return Forbid();

        answer.EarnedPoints = req.Points;
        answer.IsCorrect = req.Points > 0;
        answer.Feedback = req.Feedback;

        // Recalculate attempt score
        var allAnswers = await db.AttemptAnswers.Where(a => a.AttemptId == answer.AttemptId).ToListAsync();
        answer.Attempt.Score = allAnswers.Sum(a => a.EarnedPoints ?? 0);
        answer.Attempt.IsPassed = (double)answer.Attempt.Score / answer.Attempt.MaxScore * 100 >= answer.Attempt.Quiz.PassScore;

        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Question Bank ─────────────────────────────────────────────────────────

    [HttpGet("question-bank")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<PagedResponse<QuestionBankResponse>>> GetBank(
        [FromQuery] string? category, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = db.QuestionBank
            .Include(q => q.Options)
            .Where(q => UserRole == "admin" || q.OwnerId == UserId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(q => q.Category == category);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new PagedResponse<QuestionBankResponse>(
            items.Select(ToBankResponse), total, page, pageSize,
            (int)Math.Ceiling(total / (double)pageSize)));
    }

    [HttpPost("question-bank")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<QuestionBankResponse>> AddToBank(QuestionBankRequest req)
    {
        var userName = User.FindFirst("name")?.Value ?? string.Empty;
        var item = new QuestionBank
        {
            OwnerId = UserId,
            OwnerName = userName,
            Category = req.Category,
            Text = req.Text,
            Type = req.Type,
            Points = req.Points,
            Options = req.Options?.Select(o => new QuestionBankOption
            {
                Text = o.Text, IsCorrect = o.IsCorrect
            }).ToList() ?? []
        };
        db.QuestionBank.Add(item);
        await db.SaveChangesAsync();
        return Ok(ToBankResponse(item));
    }

    [HttpDelete("question-bank/{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> DeleteFromBank(int id)
    {
        var item = await db.QuestionBank.FindAsync(id);
        if (item == null) return NotFound();
        if (UserRole != "admin" && item.OwnerId != UserId) return Forbid();

        // Remove PracticeAttemptAnswers that reference this bank question (FK is RESTRICT/non-nullable)
        var practiceAnswers = await db.PracticeAttemptAnswers
            .Where(a => a.BankQuestionId == id)
            .ToListAsync();
        if (practiceAnswers.Count > 0)
            db.PracticeAttemptAnswers.RemoveRange(practiceAnswers);

        db.QuestionBank.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<bool> IsTeacherOrAdmin(int courseId)
    {
        if (UserRole == "admin") return true;
        if (UserRole != "teacher") return false;
        return await db.Courses.AnyAsync(c => c.Id == courseId && c.InstructorId == UserId);
    }

    private static QuizResponse ToQuizResponse(Quiz q) => new(
        q.Id, q.CourseId, q.Title, q.Description,
        q.TimeLimitMinutes, q.MaxAttempts, q.PassScore, q.DueDate,
        q.IsPublished, q.ShowAnswers, q.Questions?.Count ?? 0,
        q.Questions?.Sum(qq => qq.Points) ?? 0,
        q.Attempts?.Count ?? 0, q.CreatedAt);

    private static QuestionResponse ToQuestionResponse(Question q) => new(
        q.Id, q.QuizId, q.Text, q.Type, q.Points, q.Order,
        q.Options.Select(o => new QuestionOptionResponse(o.Id, o.Text, o.IsCorrect)).ToList());

    private static QuestionBankResponse ToBankResponse(QuestionBank q) => new(
        q.Id, q.OwnerId, q.OwnerName, q.Category, q.Text, q.Type, q.Points,
        q.Options.Select(o => new QuestionOptionResponse(o.Id, o.Text, o.IsCorrect)).ToList(),
        q.CreatedAt);
}

public record GradeEssayRequest(int Points, string? Feedback);
