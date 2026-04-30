using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

/// <summary>
/// Service-to-service endpoints untuk integrasi DWI Mobile → LMS.
/// Autentikasi pakai X-Api-Key header (bukan Keycloak JWT).
/// </summary>
[ApiController]
[Route("api/service")]
[AllowAnonymous] // Auth ditangani oleh ApiKeyAuthAttribute, bukan Keycloak
public class ServiceIntegrationController(
    LmsDbContext db,
    MandatoryExamTokenService tokenService,
    IConfiguration config) : ControllerBase
{
    // ── POST /api/service/mandatory-exams/{id}/generate-link ──────────────────
    //
    // DWI Mobile server memanggil endpoint ini untuk mendapatkan deep link ujian
    // bagi user tertentu. Assignment dibuat otomatis jika belum ada.
    //
    // Request body:
    //   { "userId": "dwi-123", "userName": "Budi Santoso", "expiryMinutes": 60 }
    //
    // Response:
    //   { "deepLink": "...", "expiresAt": "...", "sessionId": 42 }

    [HttpPost("mandatory-exams/{id:int}/generate-link")]
    [ApiKeyAuth]
    public async Task<IActionResult> GenerateExamLink(int id, ServiceGenerateLinkRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.UserId))
            return BadRequest(new { message = "userId wajib diisi." });

        // 1. Cek exam ada dan aktif
        var exam = await db.MandatoryExams.FindAsync(id);
        if (exam == null)
            return NotFound(new { message = "Exam tidak ditemukan." });
        if (!exam.IsActive)
            return BadRequest(new { message = "Exam tidak aktif.", code = "EXAM_INACTIVE" });

        // 2. Auto-create assignment jika belum ada
        var assignment = await db.MandatoryExamAssignments
            .Include(a => a.Attempts)
            .FirstOrDefaultAsync(a => a.ExamId == id && a.UserId == req.UserId);

        if (assignment == null)
        {
            assignment = new MandatoryExamAssignment
            {
                ExamId   = id,
                UserId   = req.UserId,
                UserName = req.UserName ?? req.UserId,
            };
            db.MandatoryExamAssignments.Add(assignment);
            await db.SaveChangesAsync();

            // Reload dengan Attempts
            assignment = await db.MandatoryExamAssignments
                .Include(a => a.Attempts)
                .FirstAsync(a => a.Id == assignment.Id);
        }

        // 3. Cek apakah attempt sudah habis
        var submittedCount = assignment.Attempts.Count(a => a.SubmittedAt != null);
        if (submittedCount >= exam.MaxAttempts)
            return BadRequest(new
            {
                message = $"User sudah mencapai batas {exam.MaxAttempts} percobaan.",
                code    = "ATTEMPTS_EXHAUSTED"
            });

        // 4. Generate token dan deep link
        var expiry           = Math.Clamp(req.ExpiryMinutes ?? 60, 10, 1440); // 10 menit – 24 jam
        var (token, expiresAt, jti) = tokenService.GenerateToken(req.UserId, id, expiry);

        var baseUrl  = (config["MandatoryExam:FrontendBaseUrl"] ?? "").TrimEnd('/');
        var deepLink = $"{baseUrl}/exam/start?token={Uri.EscapeDataString(token)}";

        // 5. Simpan session untuk audit trail + kemampuan revoke
        var session = new MandatoryExamSession
        {
            ExamId      = id,
            UserId      = req.UserId,
            TokenJti    = jti,
            GeneratedBy = "dwi-mobile-service",
            ExpiresAt   = expiresAt,
        };
        db.MandatoryExamSessions.Add(session);
        await db.SaveChangesAsync();

        return Ok(new
        {
            deepLink,
            expiresAt,
            sessionId      = session.Id,
            attemptUsed    = submittedCount,
            attemptLimit   = exam.MaxAttempts,
            assignmentId   = assignment.Id,
        });
    }

    // ── GET /api/service/mandatory-exams/active ───────────────────────────────
    //
    // DWI Mobile bisa query daftar exam yang sedang aktif, beserta examId-nya.
    // Berguna agar DWI Mobile tidak perlu hardcode examId.

    [HttpGet("mandatory-exams/active")]
    [ApiKeyAuth]
    public async Task<IActionResult> GetActiveExams()
    {
        var exams = await db.MandatoryExams
            .Where(e => e.IsActive)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Description,
                e.TimeLimitMinutes,
                e.MaxAttempts,
                e.PassScore,
                e.CreatedAt,
            })
            .ToListAsync();

        return Ok(exams);
    }

    // ── GET /api/service/user/{userId}/mandatory-exams ───────────────────────
    //
    // DWI Mobile menampilkan daftar semua ujian wajib yang tersedia beserta
    // status user (belum mulai / sedang / lulus / gagal / habis kesempatan).

    [HttpGet("user/{userId}/mandatory-exams")]
    [ApiKeyAuth]
    public async Task<IActionResult> GetExamsForUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(new { message = "userId wajib diisi." });

        var exams = await db.MandatoryExams
            .Where(e => e.IsActive)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new { e.Id, e.Title, e.Description, e.TimeLimitMinutes, e.MaxAttempts, e.PassScore, e.QuestionsPerAttempt })
            .ToListAsync();

        var examIds = exams.Select(e => e.Id).ToList();

        var assignments = await db.MandatoryExamAssignments
            .Include(a => a.Attempts)
            .Where(a => a.UserId == userId && examIds.Contains(a.ExamId))
            .ToListAsync();

        var assignmentMap = assignments.ToDictionary(a => a.ExamId);

        var result = exams.Select(e =>
        {
            assignmentMap.TryGetValue(e.Id, out var asgn);
            var submitted    = asgn?.Attempts.Count(a => a.SubmittedAt != null) ?? 0;
            var isPassed     = asgn?.Attempts.Any(a => a.SubmittedAt != null && a.IsPassed == true) ?? false;
            var hasActive    = asgn?.Attempts.Any(a => a.SubmittedAt == null) ?? false;
            var bestScore    = asgn?.Attempts
                .Where(a => a.SubmittedAt != null && a.Score != null)
                .Select(a => (int?)a.Score)
                .DefaultIfEmpty(null)
                .Max();

            var status = asgn == null                    ? "NotStarted"
                       : isPassed                        ? "Passed"
                       : submitted >= e.MaxAttempts      ? "Exhausted"
                       : hasActive                       ? "InProgress"
                       : submitted > 0                   ? "Failed"
                       :                                   "NotStarted";

            return new
            {
                examId          = e.Id,
                title           = e.Title,
                description     = e.Description,
                timeLimitMinutes = e.TimeLimitMinutes,
                maxAttempts     = e.MaxAttempts,
                passScore       = e.PassScore,
                questionsPerAttempt = e.QuestionsPerAttempt,
                status,
                attemptUsed     = submitted,
                isPassed,
                bestScore,
                canStart        = !isPassed && submitted < e.MaxAttempts,
            };
        }).ToList();

        return Ok(result);
    }

    // ── POST /api/service/mandatory-exams/{id}/start ──────────────────────────
    //
    // One-shot endpoint untuk DWI Mobile: buat/resume attempt + generate token +
    // kembalikan soal langsung → DWI Mobile render ujian di dalam app sendiri.
    //
    // Request body: { "userId": "dwi-123", "userName": "Budi Santoso" }
    //
    // Response: PublicAccessExamResponse (examToken, attemptId, soal, dll.)

    [HttpPost("mandatory-exams/{id:int}/start")]
    [ApiKeyAuth]
    public async Task<IActionResult> StartExam(int id, ServiceStartExamRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.UserId))
            return BadRequest(new { message = "userId wajib diisi." });

        // 1. Load exam dengan soal + opsi
        var exam = await db.MandatoryExams
            .Include(e => e.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);
        if (exam == null)
            return NotFound(new { message = "Exam tidak ditemukan atau tidak aktif." });

        // 2. Auto-create assignment jika belum ada
        var assignment = await db.MandatoryExamAssignments
            .Include(a => a.Attempts)
            .FirstOrDefaultAsync(a => a.ExamId == id && a.UserId == req.UserId);

        if (assignment == null)
        {
            assignment = new MandatoryExamAssignment
            {
                ExamId   = id,
                UserId   = req.UserId,
                UserName = req.UserName ?? req.UserId,
            };
            db.MandatoryExamAssignments.Add(assignment);
            await db.SaveChangesAsync();
            assignment = await db.MandatoryExamAssignments
                .Include(a => a.Attempts)
                .FirstAsync(a => a.Id == assignment.Id);
        }

        // 3. Jika sudah lulus, tolak
        var alreadyPassed = assignment.Attempts.Any(a => a.SubmittedAt != null && a.IsPassed == true);
        if (alreadyPassed)
            return BadRequest(new { message = "User sudah lulus ujian ini.", code = "ALREADY_PASSED" });

        // 4. Cek attempt limit
        var submittedCount = assignment.Attempts.Count(a => a.SubmittedAt != null);
        if (submittedCount >= exam.MaxAttempts)
            return BadRequest(new
            {
                message = $"User sudah mencapai batas {exam.MaxAttempts} percobaan.",
                code    = "ATTEMPTS_EXHAUSTED"
            });

        // 5. Resume attempt aktif atau buat baru
        var activeAttempt = assignment.Attempts.FirstOrDefault(a => a.SubmittedAt == null);
        bool isResume     = activeAttempt != null;

        if (!isResume)
        {
            activeAttempt = new MandatoryExamAttempt
            {
                AssignmentId = assignment.Id,
                ExamId       = exam.Id,
                UserId       = req.UserId,
                StartedAt    = DateTime.UtcNow,
            };
            db.MandatoryExamAttempts.Add(activeAttempt);
            assignment.Status = MandatoryExamAssignmentStatus.InProgress;
            await db.SaveChangesAsync();
        }

        // 6. Generate exam token — berlaku selama time limit + buffer
        var tokenMinutes = exam.TimeLimitMinutes.HasValue
            ? exam.TimeLimitMinutes.Value + 30
            : 1440;
        var (token, expiresAt, jti) = tokenService.GenerateToken(req.UserId, exam.Id, tokenMinutes);

        // 7. Simpan session
        var session = new MandatoryExamSession
        {
            ExamId      = exam.Id,
            UserId      = req.UserId,
            TokenJti    = jti,
            GeneratedBy = "dwi-mobile-service",
            ExpiresAt   = expiresAt,
            UsedAt      = DateTime.UtcNow,
        };
        db.MandatoryExamSessions.Add(session);
        await db.SaveChangesAsync();

        // 8. Pilih soal (acak atau semua, resume-aware)
        var assignedQuestions = await MandatoryExamHelper.GetAssignedQuestionsAsync(
            db, exam, activeAttempt!, isResume);

        var questions = assignedQuestions.Select((q, idx) => new MandatoryQuestionResponse(
            q.Id, q.ExamId, q.Text, q.Type.ToString(), q.Points, idx,
            q.Options.Select(o => new MandatoryOptionResponse(o.Id, o.Text, false)).ToList()
        )).ToList();

        return Ok(new PublicAccessExamResponse(
            token,
            activeAttempt!.Id, exam.Id, exam.Title, exam.Description,
            exam.TimeLimitMinutes, isResume, activeAttempt.StartedAt,
            questions));
    }

    // ── GET /api/service/mandatory-exams/{id}/user-status ─────────────────────
    //
    // DWI Mobile bisa cek status user terhadap exam tertentu sebelum generate link.
    // Berguna untuk menampilkan badge "Lulus / Belum / Sedang" di app.

    [HttpGet("mandatory-exams/{id:int}/user-status")]
    [ApiKeyAuth]
    public async Task<IActionResult> GetUserStatus(int id, [FromQuery] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(new { message = "userId wajib diisi." });

        var exam = await db.MandatoryExams.FindAsync(id);
        if (exam == null) return NotFound(new { message = "Exam tidak ditemukan." });

        var assignment = await db.MandatoryExamAssignments
            .Include(a => a.Attempts)
            .FirstOrDefaultAsync(a => a.ExamId == id && a.UserId == userId);

        if (assignment == null)
            return Ok(new { status = "NotAssigned", attemptUsed = 0, attemptLimit = exam.MaxAttempts, isPassed = (bool?)null });

        var submittedCount = assignment.Attempts.Count(a => a.SubmittedAt != null);
        var bestScore      = assignment.Attempts
            .Where(a => a.SubmittedAt != null && a.Score != null)
            .Select(a => a.Score)
            .DefaultIfEmpty(null)
            .Max();

        return Ok(new
        {
            status       = assignment.Status.ToString(),
            attemptUsed  = submittedCount,
            attemptLimit = exam.MaxAttempts,
            isPassed     = assignment.Status == MandatoryExamAssignmentStatus.Done
                           && assignment.Attempts.Any(a => a.IsPassed == true),
            bestScore,
            completedAt  = assignment.CompletedAt,
        });
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public class ServiceGenerateLinkRequest
{
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public int? ExpiryMinutes { get; set; }
}

public class ServiceStartExamRequest
{
    /// <summary>User ID dari sistem DWI Mobile.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Nama tampilan user (opsional).</summary>
    public string? UserName { get; set; }
}
