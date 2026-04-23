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
public class MandatoryExamsController(
    LmsDbContext db,
    MandatoryExamTokenService tokenService,
    IConfiguration config) : ControllerBase
{
    private string UserId   => User.FindFirst("sub")?.Value  ?? string.Empty;
    private string UserName => User.FindFirst("name")?.Value ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";
    private bool   IsAdmin  => UserRole == "admin";

    private string BuildPublicLink(string? code) =>
        code == null ? string.Empty
        : $"{(config["MandatoryExam:FrontendBaseUrl"] ?? "").TrimEnd('/')}/exam/start?code={code}";

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
            e.Questions.Count, e.Assignments.Count, e.CreatedAt,
            e.PublicAccessCode, e.PublicAccessCode == null ? null : BuildPublicLink(e.PublicAccessCode),
            e.WebhookUrl)));
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
                exam.TimeLimitMinutes, exam.MaxAttempts, exam.PassScore, exam.IsActive,
                0, 0, exam.CreatedAt, null, null, null));
    }

    // PUT /api/mandatory-exams/{id}
    [HttpPut("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> Update(int id, UpdateMandatoryExamRequest req)
    {
        var exam = await db.MandatoryExams.FindAsync(id);
        if (exam == null) return NotFound();
        if (!IsAdmin && exam.CreatedBy != UserId) return Forbid();
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { message = "Judul tidak boleh kosong." });

        exam.Title            = req.Title.Trim();
        exam.Description      = req.Description?.Trim();
        exam.TimeLimitMinutes = req.TimeLimitMinutes;
        exam.MaxAttempts      = Math.Max(1, req.MaxAttempts);
        exam.PassScore        = Math.Clamp(req.PassScore, 0, 100);
        exam.WebhookUrl       = string.IsNullOrWhiteSpace(req.WebhookUrl) ? null : req.WebhookUrl.Trim();
        await db.SaveChangesAsync();

        return Ok(new { message = "Ujian berhasil diperbarui." });
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

        var maxOrder = await db.MandatoryExamQuestions
            .Where(q => q.ExamId == id)
            .Select(q => (int?)q.Order)
            .MaxAsync();
        var order = (maxOrder ?? -1) + 1;
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

    // PUT /api/mandatory-exams/{examId}/questions/{qId}
    [HttpPut("{examId:int}/questions/{qId:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<MandatoryQuestionResponse>> UpdateQuestion(
        int examId, int qId, UpdateMandatoryQuestionRequest req)
    {
        var q = await db.MandatoryExamQuestions
            .Include(x => x.Options)
            .Include(x => x.Exam)
            .FirstOrDefaultAsync(x => x.Id == qId && x.ExamId == examId);
        if (q == null) return NotFound();
        if (!IsAdmin && q.Exam.CreatedBy != UserId) return Forbid();
        if (string.IsNullOrWhiteSpace(req.Text))
            return BadRequest(new { message = "Teks soal tidak boleh kosong." });
        if (req.Points < 1)
            return BadRequest(new { message = "Poin harus minimal 1." });
        if (q.Type == QuestionType.MultipleChoice)
        {
            if (req.Options.Count < 2)
                return BadRequest(new { message = "Pilihan ganda harus memiliki minimal 2 opsi." });
            if (!req.Options.Any(o => o.IsCorrect))
                return BadRequest(new { message = "Harus ada minimal satu opsi yang benar." });
        }

        q.Text   = req.Text.Trim();
        q.Points = req.Points;

        if (q.Type != QuestionType.Essay)
        {
            db.MandatoryExamOptions.RemoveRange(q.Options);
            q.Options = req.Options.Select(o => new MandatoryExamOption
            {
                Text      = o.Text,
                IsCorrect = o.IsCorrect,
            }).ToList();
        }

        await db.SaveChangesAsync();
        return Ok(ToQuestionRes(q));
    }

    // PUT /api/mandatory-exams/{examId}/questions/reorder
    [HttpPut("{examId:int}/questions/reorder")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> ReorderQuestions(int examId, ReorderQuestionsRequest req)
    {
        var exam = await db.MandatoryExams.FindAsync(examId);
        if (exam == null) return NotFound();
        if (!IsAdmin && exam.CreatedBy != UserId) return Forbid();

        var questions = await db.MandatoryExamQuestions
            .Where(q => q.ExamId == examId).ToListAsync();

        foreach (var item in req.Items)
        {
            var q = questions.FirstOrDefault(x => x.Id == item.QuestionId);
            if (q != null) q.Order = item.Order;
        }

        await db.SaveChangesAsync();
        return Ok(new { message = "Urutan soal berhasil diperbarui." });
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

        // Generate token dulu → dapat JTI → simpan session sekali (tidak ada double-save)
        var (token, expiresAt, jti) = tokenService.GenerateLinkToken(id, req.ExpiryMinutes);

        var session = new MandatoryExamSession
        {
            ExamId            = id,
            UserId            = "",
            TokenJti          = jti,
            GeneratedBy       = UserId,
            ExpiresAt         = expiresAt,
            IsLinkToken       = true,
            MaxUsageCount     = 5,
            CurrentUsageCount = 0,
        };
        db.MandatoryExamSessions.Add(session);
        await db.SaveChangesAsync();

        var config   = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var baseUrl  = (config["MandatoryExam:FrontendBaseUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        var deepLink = $"{baseUrl}/exam/start?token={Uri.EscapeDataString(token)}";

        return Ok(new GenerateMandatoryLinkResponse(token, deepLink, expiresAt, session.Id));
    }

    // POST /api/mandatory-exams/claim-link?linkToken=XYZ
    // Dipanggil saat user baru pertama kali klik link publik dan isi nama
    [HttpPost("claim-link")]
    [AllowAnonymous]
    public async Task<ActionResult<ClaimLinkResponse>> ClaimLink([FromQuery] string linkToken, [FromBody] ClaimLinkRequest req)
    {
        if (string.IsNullOrWhiteSpace(linkToken))
            return BadRequest(new { message = "Link token diperlukan." });

        int linkExamId; string linkJti;
        try
        {
            var parsed = tokenService.ValidateLinkToken(linkToken);
            linkExamId = parsed.ExamId;
            linkJti    = parsed.Jti;
        }
        catch (SecurityTokenExpiredException)
              { return Unauthorized(new { message = "Link sudah expired." }); }
        catch (Exception ex)
              { return Unauthorized(new { message = $"Link tidak valid: {ex.Message}" }); }

        var linkSession = await db.MandatoryExamSessions
            .FirstOrDefaultAsync(s => s.TokenJti == linkJti && s.IsLinkToken);
        if (linkSession == null)
            return Unauthorized(new { message = "Link tidak dikenal." });
        if (linkSession.IsRevoked)
            return Unauthorized(new { message = "Link sudah di-nonaktifkan." });
        if (linkSession.CurrentUsageCount >= linkSession.MaxUsageCount)
            return BadRequest(new { message = $"Link ini sudah mencapai batas maksimal {linkSession.MaxUsageCount} peserta.", code = "LIMIT_REACHED" });

        var exam = await db.MandatoryExams.FindAsync(linkExamId);
        if (exam == null || !exam.IsActive)
            return NotFound(new { message = "Exam tidak ditemukan atau tidak aktif." });

        var resolvedName = string.IsNullOrWhiteSpace(req.UserName) ? "Peserta" : req.UserName.Trim();
        var guestUserId  = Guid.NewGuid().ToString();

        // Buat assignment untuk user baru ini
        var assignment = new MandatoryExamAssignment
        {
            ExamId     = linkExamId,
            UserId     = guestUserId,
            UserName   = resolvedName,
            AssignedAt = DateTime.UtcNow,
        };
        db.MandatoryExamAssignments.Add(assignment);

        // Increment usage count
        linkSession.CurrentUsageCount++;
        if (linkSession.UsedAt == null) linkSession.UsedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        // Generate personal JWT untuk user ini (berlaku sama dengan sisa waktu link)
        var remainingMinutes = Math.Max(10, (int)(linkSession.ExpiresAt - DateTime.UtcNow).TotalMinutes);
        var (personalToken, personalExpiry, personalJti) = tokenService.GenerateToken(guestUserId, linkExamId, remainingMinutes);

        // Simpan session record untuk personal token agar ValidateToken bisa verify
        var personalSession = new MandatoryExamSession
        {
            ExamId          = linkExamId,
            UserId          = guestUserId,
            TokenJti        = personalJti,
            GeneratedBy     = "system",
            ExpiresAt       = personalExpiry,
            IsLinkToken     = false,
            ParentSessionId = linkSession.Id,
        };
        db.MandatoryExamSessions.Add(personalSession);
        await db.SaveChangesAsync();

        return Ok(new ClaimLinkResponse(
            personalToken, personalExpiry, guestUserId, linkExamId,
            linkSession.MaxUsageCount, linkSession.CurrentUsageCount
        ));
    }

    // ── Public Access Code ────────────────────────────────────────────────────

    // DELETE /api/mandatory-exams/{id}/access-code  — revoke public link
    [HttpDelete("{id:int}/access-code")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> RevokeAccessCode(int id)
    {
        var exam = await db.MandatoryExams.FindAsync(id);
        if (exam == null) return NotFound();
        if (!IsAdmin && exam.CreatedBy != UserId) return Forbid();

        exam.PublicAccessCode = null;
        await db.SaveChangesAsync();
        return Ok(new { message = "Link publik berhasil dicabut." });
    }

    // POST /api/mandatory-exams/{id}/generate-access-code
    [HttpPost("{id:int}/generate-access-code")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> GenerateAccessCode(int id)
    {
        var exam = await db.MandatoryExams.FindAsync(id);
        if (exam == null) return NotFound();
        if (!IsAdmin && exam.CreatedBy != UserId) return Forbid();
        if (!exam.IsActive) return BadRequest(new { message = "Aktifkan exam terlebih dahulu." });

        exam.PublicAccessCode = Guid.NewGuid().ToString("N"); // 32-char hex, tanpa dash
        await db.SaveChangesAsync();

        var config   = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var baseUrl  = (config["MandatoryExam:FrontendBaseUrl"] ?? "").TrimEnd('/');
        var publicLink = $"{baseUrl}/exam/start?code={exam.PublicAccessCode}";

        return Ok(new { code = exam.PublicAccessCode, publicLink });
    }

    // ── Import from Question Bank ─────────────────────────────────────────────

    // POST /api/mandatory-exams/{id}/import-questions
    [HttpPost("{id:int}/import-questions")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> ImportQuestions(int id, ImportQuestionsRequest req)
    {
        if (req.QuestionBankIds == null || req.QuestionBankIds.Count == 0)
            return BadRequest(new { message = "Pilih minimal satu soal untuk diimpor." });

        var exam = await db.MandatoryExams.FindAsync(id);
        if (exam == null) return NotFound();
        if (!IsAdmin && exam.CreatedBy != UserId) return Forbid();

        var bankQuestions = await db.CourseQuestionBanks
            .Include(q => q.Options)
            .Where(q => req.QuestionBankIds.Contains(q.Id))
            .ToListAsync();

        if (bankQuestions.Count == 0)
            return BadRequest(new { message = "Soal tidak ditemukan di bank soal." });

        var startOrder = await db.MandatoryExamQuestions.CountAsync(q => q.ExamId == id);

        var newQuestions = bankQuestions.Select((bq, i) => new MandatoryExamQuestion
        {
            ExamId  = id,
            Text    = bq.Text,
            Type    = bq.Type,
            Points  = bq.Points,
            Order   = startOrder + i,
            Options = bq.Options.Select(o => new MandatoryExamOption
            {
                Text      = o.Text,
                IsCorrect = o.IsCorrect,
            }).ToList(),
        }).ToList();

        db.MandatoryExamQuestions.AddRange(newQuestions);
        await db.SaveChangesAsync();

        return Ok(new { imported = newQuestions.Count, message = $"{newQuestions.Count} soal berhasil diimpor." });
    }

    // ── Attempts (admin results view) ────────────────────────────────────────

    // GET /api/mandatory-exams/{id}/attempts
    [HttpGet("{id:int}/attempts")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<IEnumerable<MandatoryAttemptSummaryResponse>>> GetAttempts(int id)
    {
        var exam = await db.MandatoryExams
            .Include(e => e.Questions)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (exam == null) return NotFound();
        if (!IsAdmin && exam.CreatedBy != UserId) return Forbid();

        var attempts = await db.MandatoryExamAttempts
            .Include(a => a.Assignment)
            .Include(a => a.Answers).ThenInclude(ans => ans.Question)
            .Where(a => a.ExamId == id && a.SubmittedAt != null)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();

        var results = attempts.Select(a =>
        {
            var pct = (a.MaxScore ?? 0) > 0
                ? (int)Math.Round((double)(a.Score ?? 0) / a.MaxScore!.Value * 100)
                : 0;

            var essayAnswers = a.Answers
                .Where(ans => ans.Question.Type == QuestionType.Essay)
                .Select(ans => new MandatoryEssayAnswerAdminResponse(
                    ans.Id, ans.QuestionId, ans.Question.Text, ans.Question.Points,
                    ans.EssayAnswer, ans.EarnedPoints, ans.Feedback))
                .ToList();

            return new MandatoryAttemptSummaryResponse(
                a.Id, a.UserId, a.Assignment.UserName,
                a.Score, a.MaxScore, pct, a.IsPassed,
                a.StartedAt, a.SubmittedAt, essayAnswers);
        });

        return Ok(results);
    }

    // GET /api/mandatory-exams/{id}/export  — download hasil sebagai CSV
    [HttpGet("{id:int}/export")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> ExportResults(int id)
    {
        var exam = await db.MandatoryExams.FindAsync(id);
        if (exam == null) return NotFound();
        if (!IsAdmin && exam.CreatedBy != UserId) return Forbid();

        var attempts = await db.MandatoryExamAttempts
            .Include(a => a.Assignment)
            .Where(a => a.ExamId == id && a.SubmittedAt != null)
            .OrderBy(a => a.Assignment.UserName).ThenBy(a => a.StartedAt)
            .ToListAsync();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("No,Nama,User ID,Skor,Maks Skor,Persentase (%),Lulus,Percobaan Ke,Mulai,Selesai,Durasi (menit)");

        // Group by userId untuk hitung percobaan ke-n
        var attemptNo = new Dictionary<string, int>();
        var no = 1;
        foreach (var a in attempts)
        {
            if (!attemptNo.ContainsKey(a.UserId)) attemptNo[a.UserId] = 0;
            attemptNo[a.UserId]++;

            var pct      = (a.MaxScore ?? 0) > 0 ? Math.Round((double)(a.Score ?? 0) / a.MaxScore!.Value * 100, 1) : 0;
            var durasi   = a.SubmittedAt.HasValue ? Math.Round((a.SubmittedAt.Value - a.StartedAt).TotalMinutes, 1) : 0;
            var userName = $"\"{a.Assignment.UserName}\"";

            sb.AppendLine(string.Join(",",
                no++,
                userName,
                a.UserId,
                a.Score ?? 0,
                a.MaxScore ?? 0,
                pct,
                a.IsPassed == true ? "Ya" : "Tidak",
                attemptNo[a.UserId],
                a.StartedAt.ToString("yyyy-MM-dd HH:mm"),
                a.SubmittedAt?.ToString("yyyy-MM-dd HH:mm") ?? "",
                durasi));
        }

        var fileName = $"hasil-ujian-{exam.Title.Replace(" ", "_")}-{DateTime.UtcNow:yyyyMMdd}.csv";
        var bytes    = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();

        return File(bytes, "text/csv", fileName);
    }

    // PATCH /api/mandatory-exams/attempts/{attemptId}/answers/{answerId}/grade
    [HttpPatch("attempts/{attemptId:int}/answers/{answerId:int}/grade")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> GradeEssayAnswer(int attemptId, int answerId, GradeEssayAnswerRequest req)
    {
        var answer = await db.MandatoryExamAnswers
            .Include(a => a.Attempt).ThenInclude(at => at.Assignment).ThenInclude(a => a.Exam)
            .Include(a => a.Question)
            .FirstOrDefaultAsync(a => a.Id == answerId && a.AttemptId == attemptId);

        if (answer == null) return NotFound();
        if (answer.Question.Type != QuestionType.Essay)
            return BadRequest(new { message = "Hanya soal essay yang bisa dinilai manual." });

        var exam = answer.Attempt.Assignment.Exam;
        if (!IsAdmin && exam.CreatedBy != UserId) return Forbid();

        var maxPts = answer.Question.Points;
        answer.EarnedPoints = Math.Clamp(req.EarnedPoints, 0, maxPts);
        answer.Feedback     = req.Feedback;
        answer.IsCorrect    = answer.EarnedPoints == maxPts;

        // Recalculate attempt score
        var attempt = answer.Attempt;
        var allAnswers = await db.MandatoryExamAnswers
            .Where(a => a.AttemptId == attempt.Id).ToListAsync();
        attempt.Score = allAnswers.Sum(a => a.EarnedPoints ?? 0);

        var pct = (attempt.MaxScore ?? 0) > 0
            ? (int)Math.Round((double)attempt.Score.Value / attempt.MaxScore!.Value * 100)
            : 0;
        attempt.IsPassed = pct >= exam.PassScore;

        // Update assignment status if now passed
        if (attempt.IsPassed == true)
        {
            var assignment = attempt.Assignment;
            assignment.Status      = MandatoryExamAssignmentStatus.Done;
            assignment.CompletedAt ??= DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return Ok(new { score = attempt.Score, percentage = pct, isPassed = attempt.IsPassed });
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
public class MandatoryExamSessionController(
    LmsDbContext db,
    MandatoryExamTokenService tokenService,
    IHttpClientFactory httpClientFactory,
    ILogger<MandatoryExamSessionController> logger) : ControllerBase
{
    // ── Access by Public Code → start or resume attempt ──────────────────────
    //
    // DWI Mobile membuka URL: /exam/start?code=XXX&userId=YYY&userName=ZZZ
    // Frontend panggil endpoint ini untuk mendapatkan exam token + soal.

    // GET /api/mandatory-exams/access?code=X&userId=Y&userName=Z
    [HttpGet("mandatory-exams/access")]
    public async Task<ActionResult<PublicAccessExamResponse>> AccessByCode(
        [FromQuery] string code,
        [FromQuery] string userId,
        [FromQuery] string? userName)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { message = "Parameter 'code' diperlukan." });
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(new { message = "Parameter 'userId' diperlukan." });

        // 1. Cari exam berdasarkan public access code
        var exam = await db.MandatoryExams
            .Include(e => e.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(e => e.PublicAccessCode == code);

        if (exam == null)
            return NotFound(new { message = "Link ujian tidak valid." });
        if (!exam.IsActive)
            return BadRequest(new { message = "Ujian ini sedang tidak aktif.", code = "EXAM_INACTIVE" });

        // 2. Auto-create assignment jika belum ada
        var assignment = await db.MandatoryExamAssignments
            .Include(a => a.Attempts)
            .FirstOrDefaultAsync(a => a.ExamId == exam.Id && a.UserId == userId);

        if (assignment == null)
        {
            assignment = new MandatoryExamAssignment
            {
                ExamId   = exam.Id,
                UserId   = userId,
                UserName = userName ?? userId,
            };
            db.MandatoryExamAssignments.Add(assignment);
            await db.SaveChangesAsync();

            assignment = await db.MandatoryExamAssignments
                .Include(a => a.Attempts)
                .FirstAsync(a => a.Id == assignment.Id);
        }

        // 3. Cek attempt limit
        var submittedCount = assignment.Attempts.Count(a => a.SubmittedAt != null);
        if (submittedCount >= exam.MaxAttempts)
            return BadRequest(new { message = "Percobaan sudah habis.", code = "ATTEMPTS_EXHAUSTED" });

        // 4. Resume attempt aktif atau buat baru
        var activeAttempt = assignment.Attempts.FirstOrDefault(a => a.SubmittedAt == null);
        bool isResume = activeAttempt != null;

        if (!isResume)
        {
            activeAttempt = new MandatoryExamAttempt
            {
                AssignmentId = assignment.Id,
                ExamId       = exam.Id,
                UserId       = userId,
                StartedAt    = DateTime.UtcNow,
            };
            db.MandatoryExamAttempts.Add(activeAttempt);
            assignment.Status = MandatoryExamAssignmentStatus.InProgress;
            await db.SaveChangesAsync();
        }

        // 5. Generate exam token (JWT) untuk submit — berlaku 24 jam atau 2x time limit
        var tokenMinutes = exam.TimeLimitMinutes.HasValue
            ? exam.TimeLimitMinutes.Value + 30  // sedikit buffer
            : 1440;                              // default 24 jam
        var (token, _, jti) = tokenService.GenerateToken(userId, exam.Id, tokenMinutes);

        // 6. Simpan session
        var session = new MandatoryExamSession
        {
            ExamId      = exam.Id,
            UserId      = userId,
            TokenJti    = jti,
            GeneratedBy = "public-link",
            ExpiresAt   = DateTime.UtcNow.AddMinutes(tokenMinutes),
            UsedAt      = DateTime.UtcNow,
        };
        db.MandatoryExamSessions.Add(session);
        await db.SaveChangesAsync();

        // 7. Sembunyikan IsCorrect dari student
        var questions = exam.Questions.OrderBy(q => q.Order).Select(q => new MandatoryQuestionResponse(
            q.Id, q.ExamId, q.Text, q.Type.ToString(), q.Points, q.Order,
            q.Options.Select(o => new MandatoryOptionResponse(o.Id, o.Text, false)).ToList()
        )).ToList();

        return Ok(new PublicAccessExamResponse(
            token,
            activeAttempt!.Id, exam.Id, exam.Title, exam.Description,
            exam.TimeLimitMinutes, isResume, activeAttempt.StartedAt,
            questions));
    }

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

        // 2. Validasi assignment (dibuat oleh ClaimLink untuk public link, atau pre-assigned)
        var assignment = await db.MandatoryExamAssignments
            .Include(a => a.Attempts)
            .FirstOrDefaultAsync(a => a.ExamId == examId && a.UserId == userId);
        if (assignment == null)
            return StatusCode(403, new { message = "Akses tidak diizinkan. Gunakan link yang valid." });

        // Cek apakah sudah lulus — jika ya, tidak bisa mengulang
        var alreadyPassed = assignment.Attempts.Any(a => a.SubmittedAt != null && a.IsPassed == true);
        if (alreadyPassed)
            return BadRequest(new { message = "Anda sudah lulus ujian ini. Tidak perlu mengulang.", code = "ALREADY_PASSED" });

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

        var totalSubmitted = await db.MandatoryExamAttempts
            .CountAsync(a => a.AssignmentId == assignment.Id && a.SubmittedAt != null);
        var remaining = Math.Max(0, exam.MaxAttempts - totalSubmitted);

        // Fire webhook (fire-and-forget — jangan blokir response user)
        if (!string.IsNullOrWhiteSpace(exam.WebhookUrl))
            _ = FireWebhookAsync(exam, attempt, pct, isPassed);

        return Ok(BuildResult(attempt, exam, answers, remaining));
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
            .Include(a => a.Assignment).ThenInclude(a => a.Attempts)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (attempt == null) return NotFound();
        if (attempt.SubmittedAt == null) return BadRequest(new { message = "Attempt belum di-submit." });

        var exam = await db.MandatoryExams
            .Include(e => e.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(e => e.Id == attempt.ExamId);
        if (exam == null) return NotFound();

        var totalSubmitted = attempt.Assignment.Attempts.Count(a => a.SubmittedAt != null);
        var remaining      = Math.Max(0, exam.MaxAttempts - totalSubmitted);

        return Ok(BuildResult(attempt, exam, attempt.Answers.ToList(), remaining));
    }

    // ── Webhook ───────────────────────────────────────────────────────────────

    private async Task FireWebhookAsync(MandatoryExam exam, MandatoryExamAttempt attempt, int pct, bool isPassed)
    {
        try
        {
            var client  = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var payload = new
            {
                @event      = "exam_submitted",
                examId      = exam.Id,
                examTitle   = exam.Title,
                userId      = attempt.UserId,
                score       = attempt.Score ?? 0,
                maxScore    = attempt.MaxScore ?? 0,
                percentage  = pct,
                isPassed,
                submittedAt = attempt.SubmittedAt?.ToString("o"),
            };

            using var content = new System.Net.Http.StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json");

            await client.PostAsync(exam.WebhookUrl, content);
        }
        catch (Exception ex)
        {
            logger.LogWarning("[Webhook] Gagal kirim ke {Url} untuk exam {ExamId}: {Message}",
                exam.WebhookUrl, exam.Id, ex.Message);
        }
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
        MandatoryExamAttempt attempt, MandatoryExam exam,
        List<MandatoryExamAnswer> answers, int remainingAttempts = 0)
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
            exam.MaxAttempts, remainingAttempts,
            attempt.StartedAt, attempt.SubmittedAt!.Value,
            answerResults);
    }
}
