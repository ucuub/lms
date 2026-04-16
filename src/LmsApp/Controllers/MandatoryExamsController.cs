using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LmsApp.Controllers;

// ══════════════════════════════════════════════════════════════════════════════
//  MANAGEMENT — requires Keycloak JWT (teacher / admin)
// ══════════════════════════════════════════════════════════════════════════════

[ApiController]
[Route("api/mandatory-exams")]
[Authorize]
public class MandatoryExamsController(LmsDbContext db, MandatoryExamTokenService tokenService) : ControllerBase
{
    private string UserId   => User.FindFirst("sub")?.Value  ?? string.Empty;
    private string UserName => User.FindFirst("name")?.Value ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";
    private bool   IsAdmin  => UserRole == "admin";

    // ── CRUD ──────────────────────────────────────────────────────────────────

    // GET /api/mandatory-exams
    [HttpGet]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<IEnumerable<MandatoryExamSummaryResponse>>> GetAll()
    {
        var exams = await db.MandatoryExams
            .Include(e => e.Questions)
            .Include(e => e.Assignments)
            .Where(e => IsAdmin || e.CreatedBy == UserId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return Ok(exams.Select(e => new MandatoryExamSummaryResponse(
            e.Id, e.Title, e.Description, e.TimeLimitMinutes,
            e.MaxAttempts, e.PassScore, e.IsActive,
            e.Questions.Count, e.Assignments.Count, e.CreatedAt)));
    }

    // GET /api/mandatory-exams/{id}
    [HttpGet("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<MandatoryExamDetailResponse>> GetById(int id)
    {
        var e = await db.MandatoryExams
            .Include(x => x.Questions).ThenInclude(q => q.Options)
            .Include(x => x.Assignments).ThenInclude(a => a.Attempts)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (e == null) return NotFound();
        if (!IsAdmin && e.CreatedBy != UserId) return Forbid();
        return Ok(ToDetail(e));
    }

    // POST /api/mandatory-exams
    [HttpPost]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<MandatoryExamSummaryResponse>> Create(CreateMandatoryExamRequest req)
    {
        var exam = new MandatoryExam
        {
            Title            = req.Title,
            Description      = req.Description,
            TimeLimitMinutes = req.TimeLimitMinutes,
            MaxAttempts      = Math.Max(1, req.MaxAttempts),
            PassScore        = Math.Clamp(req.PassScore, 0, 100),
            CreatedBy        = UserId,
            CreatedByName    = UserName,
        };
        db.MandatoryExams.Add(exam);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = exam.Id },
            new MandatoryExamSummaryResponse(exam.Id, exam.Title, exam.Description,
                exam.TimeLimitMinutes, exam.MaxAttempts, exam.PassScore, exam.IsActive, 0, 0, exam.CreatedAt));
    }

    // PATCH /api/mandatory-exams/{id}/toggle-active
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var exam = await db.MandatoryExams.FindAsync(id);
        if (exam == null) return NotFound();
        if (!IsAdmin && exam.CreatedBy != UserId) return Forbid();
        exam.IsActive = !exam.IsActive;
        await db.SaveChangesAsync();
        return Ok(new { isActive = exam.IsActive });
    }

    // DELETE /api/mandatory-exams/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var exam = await db.MandatoryExams.FindAsync(id);
        if (exam == null) return NotFound();
        if (!IsAdmin && exam.CreatedBy != UserId) return Forbid();

        // Delete answers first (RESTRICT FK on QuestionId)
        var attemptIds = await db.MandatoryExamAttempts
            .Where(a => a.ExamId == id).Select(a => a.Id).ToListAsync();
        if (attemptIds.Count > 0)
        {
            var answers = await db.MandatoryExamAnswers
                .Where(a => attemptIds.Contains(a.AttemptId)).ToListAsync();
            db.MandatoryExamAnswers.RemoveRange(answers);
            await db.SaveChangesAsync();
        }

        db.MandatoryExams.Remove(exam);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Questions ─────────────────────────────────────────────────────────────

    // POST /api/mandatory-exams/{id}/questions
    [HttpPost("{id:int}/questions")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<MandatoryQuestionResponse>> AddQuestion(int id, AddMandatoryQuestionRequest req)
    {
        var exam = await db.MandatoryExams.FindAsync(id);
        if (exam == null) return NotFound();
        if (!IsAdmin && exam.CreatedBy != UserId) return Forbid();
        if (!Enum.TryParse<QuestionType>(req.Type, true, out var qType))
            return BadRequest(new { message = "Tipe soal tidak valid." });

        var order = await db.MandatoryExamQuestions.CountAsync(q => q.ExamId == id);
        var q = new MandatoryExamQuestion
        {
            ExamId  = id,
            Text    = req.Text,
            Type    = qType,
            Points  = Math.Max(1, req.Points),
            Order   = order,
            Options = req.Options.Select(o => new MandatoryExamOption { Text = o.Text, IsCorrect = o.IsCorrect }).ToList()
        };
        db.MandatoryExamQuestions.Add(q);
        await db.SaveChangesAsync();
        return Ok(ToQuestionRes(q));
    }

    // DELETE /api/mandatory-exams/{examId}/questions/{qId}
    [HttpDelete("{examId:int}/questions/{qId:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> DeleteQuestion(int examId, int qId)
    {
        var q = await db.MandatoryExamQuestions
            .Include(x => x.Exam)
            .FirstOrDefaultAsync(x => x.Id == qId && x.ExamId == examId);
        if (q == null) return NotFound();
        if (!IsAdmin && q.Exam.CreatedBy != UserId) return Forbid();
        db.MandatoryExamQuestions.Remove(q);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Assignments ───────────────────────────────────────────────────────────

    // POST /api/mandatory-exams/{id}/assign
    [HttpPost("{id:int}/assign")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<MandatoryAssignmentResponse>> Assign(int id, AssignMandatoryExamRequest req)
    {
        var exam = await db.MandatoryExams.FindAsync(id);
        if (exam == null) return NotFound();
        if (!IsAdmin && exam.CreatedBy != UserId) return Forbid();

        var existing = await db.MandatoryExamAssignments
            .FirstOrDefaultAsync(a => a.ExamId == id && a.UserId == req.UserId);
        if (existing != null) return Ok(ToAssignmentRes(existing, 0));

        var assignment = new MandatoryExamAssignment
        {
            ExamId   = id,
            UserId   = req.UserId,
            UserName = req.UserName,
        };
        db.MandatoryExamAssignments.Add(assignment);
        await db.SaveChangesAsync();
        return Ok(ToAssignmentRes(assignment, 0));
    }

    // DELETE /api/mandatory-exams/{id}/assign/{userId}
    [HttpDelete("{id:int}/assign/{userId}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> Unassign(int id, string userId)
    {
        var assignment = await db.MandatoryExamAssignments
            .Include(a => a.Exam)
            .FirstOrDefaultAsync(a => a.ExamId == id && a.UserId == userId);
        if (assignment == null) return NotFound();
        if (!IsAdmin && assignment.Exam.CreatedBy != UserId) return Forbid();
        db.MandatoryExamAssignments.Remove(assignment);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // GET /api/mandatory-exams/{id}/assignments
    [HttpGet("{id:int}/assignments")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<IEnumerable<MandatoryAssignmentResponse>>> GetAssignments(int id)
    {
        var exam = await db.MandatoryExams.FindAsync(id);
        if (exam == null) return NotFound();
        if (!IsAdmin && exam.CreatedBy != UserId) return Forbid();

        var assignments = await db.MandatoryExamAssignments
            .Include(a => a.Attempts)
            .Where(a => a.ExamId == id)
            .ToListAsync();

        return Ok(assignments.Select(a => ToAssignmentRes(a, a.Attempts.Count)));
    }

    // ── Generate Deep Link ────────────────────────────────────────────────────

    // POST /api/mandatory-exams/{id}/generate-link
    [HttpPost("{id:int}/generate-link")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<GenerateMandatoryLinkResponse>> GenerateLink(int id, GenerateMandatoryLinkRequest req)
    {
        var exam = await db.MandatoryExams.FindAsync(id);
        if (exam == null) return NotFound();
        if (!IsAdmin && exam.CreatedBy != UserId) return Forbid();
        if (!exam.IsActive) return BadRequest(new { message = "Exam tidak aktif." });

        var assignment = await db.MandatoryExamAssignments
            .FirstOrDefaultAsync(a => a.ExamId == id && a.UserId == req.UserId);
        if (assignment == null)
            return BadRequest(new { message = "User belum di-assign ke exam ini." });

        var (token, expiresAt, jti) = tokenService.GenerateToken(req.UserId, id, req.ExpiryMinutes);

        var config   = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var baseUrl  = (config["MandatoryExam:FrontendBaseUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        var deepLink = $"{baseUrl}/exam/start?token={Uri.EscapeDataString(token)}";

        // Simpan session untuk audit trail + kemampuan revoke
        var session = new MandatoryExamSession
        {
            ExamId      = id,
            UserId      = req.UserId,
            TokenJti    = jti,
            GeneratedBy = UserId,
            ExpiresAt   = expiresAt,
        };
        db.MandatoryExamSessions.Add(session);
        await db.SaveChangesAsync();

        return Ok(new GenerateMandatoryLinkResponse(token, deepLink, expiresAt, session.Id));
    }

    // ── Sessions ──────────────────────────────────────────────────────────────

    // GET /api/mandatory-exams/{id}/sessions
    [HttpGet("{id:int}/sessions")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<IEnumerable<MandatoryExamSessionResponse>>> GetSessions(int id)
    {
        var exam = await db.MandatoryExams.FindAsync(id);
        if (exam == null) return NotFound();
        if (!IsAdmin && exam.CreatedBy != UserId) return Forbid();

        var sessions = await db.MandatoryExamSessions
            .Where(s => s.ExamId == id)
            .OrderByDescending(s => s.GeneratedAt)
            .ToListAsync();

        return Ok(sessions.Select(ToSessionRes));
    }

    // POST /api/mandatory-exams/sessions/{sessionId}/revoke
    [HttpPost("sessions/{sessionId:int}/revoke")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> RevokeSession(int sessionId)
    {
        var session = await db.MandatoryExamSessions
            .Include(s => s.Exam)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session == null) return NotFound();
        if (!IsAdmin && session.Exam.CreatedBy != UserId) return Forbid();
        if (session.IsRevoked) return BadRequest(new { message = "Session sudah di-revoke." });

        session.IsRevoked = true;
        await db.SaveChangesAsync();
        return Ok(new { message = "Session berhasil di-revoke." });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static MandatoryExamDetailResponse ToDetail(MandatoryExam e) => new(
        e.Id, e.Title, e.Description, e.TimeLimitMinutes, e.MaxAttempts, e.PassScore,
        e.IsActive, e.CreatedByName, e.CreatedAt,
        e.Questions.OrderBy(q => q.Order).Select(ToQuestionRes).ToList(),
        e.Assignments.Select(a => ToAssignmentRes(a, a.Attempts.Count)).ToList());

    private static MandatoryQuestionResponse ToQuestionRes(MandatoryExamQuestion q) => new(
        q.Id, q.ExamId, q.Text, q.Type.ToString(), q.Points, q.Order,
        q.Options.Select(o => new MandatoryOptionResponse(o.Id, o.Text, o.IsCorrect)).ToList());

    private static MandatoryAssignmentResponse ToAssignmentRes(MandatoryExamAssignment a, int attemptCount) => new(
        a.Id, a.UserId, a.UserName, a.Status.ToString(), a.AssignedAt, a.CompletedAt, attemptCount);

    private static MandatoryExamSessionResponse ToSessionRes(MandatoryExamSession s) => new(
        s.Id, s.ExamId, s.UserId, s.GeneratedBy,
        s.GeneratedAt, s.ExpiresAt, s.UsedAt,
        s.IsRevoked, DateTime.UtcNow > s.ExpiresAt);
}

// ══════════════════════════════════════════════════════════════════════════════
//  SESSION — AllowAnonymous, authenticated by X-Exam-Token header
// ══════════════════════════════════════════════════════════════════════════════

[ApiController]
[Route("api")]
[AllowAnonymous]
public class MandatoryExamSessionController(LmsDbContext db, MandatoryExamTokenService tokenService) : ControllerBase
{
    // ── Validate Token → start or resume attempt ──────────────────────────────

    // GET /api/mandatory-exams/validate-token?token=XYZ
    [HttpGet("mandatory-exams/validate-token")]
    public async Task<ActionResult<ValidateMandatoryTokenResponse>> ValidateToken([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { message = "Token diperlukan." });

        (string userId, int examId, string jti) parsed;
        try   { parsed = tokenService.ValidateToken(token); }
        catch (SecurityTokenExpiredException)
              { return Unauthorized(new { message = "Token sudah expired." }); }
        catch (Exception ex)
              { return Unauthorized(new { message = $"Token tidak valid: {ex.Message}" }); }

        var (userId, examId, jti) = parsed;

        // 1. Cek session — revoke check
        var examSession = await db.MandatoryExamSessions
            .FirstOrDefaultAsync(s => s.TokenJti == jti);
        if (examSession == null)
            return Unauthorized(new { message = "Token tidak dikenal." });
        if (examSession.IsRevoked)
            return Unauthorized(new { message = "Link sudah di-nonaktifkan." });

        // Set UsedAt jika pertama kali dipakai
        if (examSession.UsedAt == null)
        {
            examSession.UsedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        // 2. Validasi assignment
        var assignment = await db.MandatoryExamAssignments
            .Include(a => a.Attempts)
            .FirstOrDefaultAsync(a => a.ExamId == examId && a.UserId == userId);
        if (assignment == null)
            return StatusCode(403, new { message = "Anda tidak memiliki akses ke exam ini." });

        // 2. Load exam
        var exam = await db.MandatoryExams
            .Include(e => e.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(e => e.Id == examId && e.IsActive);
        if (exam == null)
            return NotFound(new { message = "Exam tidak ditemukan atau tidak aktif." });

        // 3. Single active attempt — resume jika ada
        var activeAttempt = assignment.Attempts.FirstOrDefault(a => a.SubmittedAt == null);
        bool isResume = activeAttempt != null;

        if (!isResume)
        {
            // 4. Attempt limit — hitung total submitted
            var submittedCount = assignment.Attempts.Count(a => a.SubmittedAt != null);
            if (submittedCount >= exam.MaxAttempts)
                return BadRequest(new { message = "Percobaan sudah habis.", code = "ATTEMPTS_EXHAUSTED" });

            // 5. Create new attempt
            activeAttempt = new MandatoryExamAttempt
            {
                AssignmentId = assignment.Id,
                ExamId       = examId,
                UserId       = userId,
                StartedAt    = DateTime.UtcNow,
            };
            db.MandatoryExamAttempts.Add(activeAttempt);
            assignment.Status = MandatoryExamAssignmentStatus.InProgress;
            await db.SaveChangesAsync();
        }

        // Hide IsCorrect from student
        var questions = exam.Questions.OrderBy(q => q.Order).Select(q => new MandatoryQuestionResponse(
            q.Id, q.ExamId, q.Text, q.Type.ToString(), q.Points, q.Order,
            q.Options.Select(o => new MandatoryOptionResponse(o.Id, o.Text, false)).ToList()
        )).ToList();

        return Ok(new ValidateMandatoryTokenResponse(
            activeAttempt!.Id, exam.Id, exam.Title, exam.Description,
            exam.TimeLimitMinutes, isResume, activeAttempt.StartedAt, questions));
    }

    // ── Submit ────────────────────────────────────────────────────────────────

    // POST /api/mandatory-exam-attempts/{id}/submit
    [HttpPost("mandatory-exam-attempts/{id:int}/submit")]
    public async Task<ActionResult<MandatoryExamResultResponse>> Submit(
        int id,
        SubmitMandatoryExamRequest req,
        [FromHeader(Name = "X-Exam-Token")] string? examToken)
    {
        var (userId, examId, _) = ValidateSessionToken(examToken, out var err);
        if (err != null) return err;

        var attempt = await db.MandatoryExamAttempts
            .Include(a => a.Assignment)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId && a.ExamId == examId);
        if (attempt == null) return NotFound(new { message = "Attempt tidak ditemukan." });
        if (attempt.SubmittedAt != null) return BadRequest(new { message = "Attempt sudah di-submit." });

        var exam = await db.MandatoryExams
            .Include(e => e.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(e => e.Id == examId);
        if (exam == null) return NotFound();

        // Score
        var answers  = new List<MandatoryExamAnswer>();
        int total    = 0;
        int maxScore = exam.Questions.Sum(q => q.Points);

        foreach (var q in exam.Questions)
        {
            var submitted = req.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
            var answer = new MandatoryExamAnswer
            {
                AttemptId        = attempt.Id,
                QuestionId       = q.Id,
                SelectedOptionId = submitted?.SelectedOptionId,
                EssayAnswer      = submitted?.EssayAnswer,
            };

            if (q.Type == QuestionType.Essay)
            {
                answer.IsCorrect    = null; // manual grading
                answer.EarnedPoints = 0;
            }
            else
            {
                var opt = q.Options.FirstOrDefault(o => o.Id == submitted?.SelectedOptionId);
                answer.IsCorrect    = opt?.IsCorrect ?? false;
                answer.EarnedPoints = answer.IsCorrect == true ? q.Points : 0;
                total              += answer.EarnedPoints ?? 0;
            }

            answers.Add(answer);
        }

        db.MandatoryExamAnswers.AddRange(answers);

        var pct      = maxScore > 0 ? (int)Math.Round((double)total / maxScore * 100) : 0;
        var isPassed = pct >= exam.PassScore;

        attempt.SubmittedAt = DateTime.UtcNow;
        attempt.Score       = total;
        attempt.MaxScore    = maxScore;
        attempt.IsPassed    = isPassed;

        // Update assignment status
        var assignment     = attempt.Assignment;
        var submitted2     = await db.MandatoryExamAttempts
            .CountAsync(a => a.AssignmentId == assignment.Id && a.SubmittedAt != null);
        var newSubmitted   = submitted2 + 1; // +1 karena ini belum tersimpan

        if (isPassed || newSubmitted >= exam.MaxAttempts)
        {
            assignment.Status      = MandatoryExamAssignmentStatus.Done;
            assignment.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            assignment.Status = MandatoryExamAssignmentStatus.InProgress;
        }

        await db.SaveChangesAsync();

        return Ok(BuildResult(attempt, exam, answers));
    }

    // ── Result ────────────────────────────────────────────────────────────────

    // GET /api/mandatory-exam-attempts/{id}/result
    [HttpGet("mandatory-exam-attempts/{id:int}/result")]
    public async Task<ActionResult<MandatoryExamResultResponse>> GetResult(
        int id,
        [FromHeader(Name = "X-Exam-Token")] string? examToken)
    {
        var (userId, _, _jti) = ValidateSessionToken(examToken, out var err);
        if (err != null) return err;

        var attempt = await db.MandatoryExamAttempts
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (attempt == null) return NotFound();
        if (attempt.SubmittedAt == null) return BadRequest(new { message = "Attempt belum di-submit." });

        var exam = await db.MandatoryExams
            .Include(e => e.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(e => e.Id == attempt.ExamId);
        if (exam == null) return NotFound();

        return Ok(BuildResult(attempt, exam, attempt.Answers.ToList()));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private (string userId, int examId, string jti) ValidateSessionToken(string? token, out ActionResult? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(token))
        {
            error = Unauthorized(new { message = "X-Exam-Token header diperlukan." });
            return (string.Empty, 0, string.Empty);
        }
        try
        {
            return tokenService.ValidateToken(token);
        }
        catch (SecurityTokenExpiredException)
        {
            error = Unauthorized(new { message = "Token sudah expired." });
            return (string.Empty, 0, string.Empty);
        }
        catch
        {
            error = Unauthorized(new { message = "Token tidak valid." });
            return (string.Empty, 0, string.Empty);
        }
    }

    private static MandatoryExamResultResponse BuildResult(
        MandatoryExamAttempt attempt, MandatoryExam exam, List<MandatoryExamAnswer> answers)
    {
        var pct = attempt.MaxScore > 0
            ? (int)Math.Round((double)(attempt.Score ?? 0) / (attempt.MaxScore ?? 1) * 100)
            : 0;

        var answerResults = exam.Questions.OrderBy(q => q.Order).Select(q =>
        {
            var ans     = answers.FirstOrDefault(a => a.QuestionId == q.Id);
            var correct = q.Options.FirstOrDefault(o => o.IsCorrect);
            var selOpt  = q.Options.FirstOrDefault(o => o.Id == ans?.SelectedOptionId);
            return new MandatoryAnswerResultResponse(
                q.Id, q.Text, q.Type.ToString(), q.Points,
                ans?.SelectedOptionId, selOpt?.Text,
                ans?.EssayAnswer,
                ans?.IsCorrect, ans?.EarnedPoints, ans?.Feedback,
                correct?.Text);
        }).ToList();

        return new MandatoryExamResultResponse(
            attempt.Id, exam.Id, exam.Title,
            attempt.Score ?? 0, attempt.MaxScore ?? 0, pct,
            attempt.IsPassed ?? false, exam.PassScore,
            attempt.StartedAt, attempt.SubmittedAt!.Value,
            answerResults);
    }
}
