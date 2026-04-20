using LmsApp.Data;
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

// ── Request DTO ───────────────────────────────────────────────────────────────

public class ServiceGenerateLinkRequest
{
    /// <summary>User ID dari sistem DWI Mobile.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Nama tampilan user (opsional — dipakai jika assignment baru dibuat).</summary>
    public string? UserName { get; set; }

    /// <summary>Berapa menit link berlaku. Default 60, min 10, max 1440 (24 jam).</summary>
    public int? ExpiryMinutes { get; set; }
}
