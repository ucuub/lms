using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[ApiController]
[Authorize]
public class CertificatesController(LmsDbContext db, ICompletionService completionService) : ControllerBase
{
    private string UserId   => User.FindFirst("sub")?.Value  ?? string.Empty;
    private string UserName => User.FindFirst("name")?.Value ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";

    // ════════════════════════════════════════════════════════════════════════
    // COMPLETION RULE — dikelola instructor
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// GET /api/courses/{courseId}/completion-rule
    /// Ambil aturan completion course. Jika belum diset, kembalikan default.
    /// </summary>
    [HttpGet("api/courses/{courseId:int}/completion-rule")]
    [AllowAnonymous]
    public async Task<ActionResult<CompletionRuleDto>> GetRule(int courseId)
    {
        var rule = await db.CourseCompletionRules
            .FirstOrDefaultAsync(r => r.CourseId == courseId);

        // Kembalikan default jika belum diset
        var dto = rule != null
            ? new CompletionRuleDto(
                rule.CourseId, rule.RequiredModulePercent,
                rule.RequireAllAssignments, rule.RequireAllQuizzesPassed, rule.UpdatedAt)
            : new CompletionRuleDto(courseId, 100, false, false, DateTime.UtcNow);

        return Ok(dto);
    }

    /// <summary>
    /// PUT /api/courses/{courseId}/completion-rule
    /// Set atau update aturan completion. Hanya instructor atau admin.
    ///
    /// Contoh request:
    /// {
    ///   "requiredModulePercent": 80,
    ///   "requireAllAssignments": true,
    ///   "requireAllQuizzesPassed": false
    /// }
    /// </summary>
    [HttpPut("api/courses/{courseId:int}/completion-rule")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<CompletionRuleDto>> SetRule(
        int courseId, SetCompletionRuleRequest req)
    {
        var course = await db.Courses.FindAsync(courseId);
        if (course == null) return NotFound(new { message = "Course tidak ditemukan." });

        if (UserRole != "admin" && course.InstructorId != UserId)
            return Forbid();

        var rule = await db.CourseCompletionRules
            .FirstOrDefaultAsync(r => r.CourseId == courseId);

        if (rule == null)
        {
            rule = new CourseCompletionRule { CourseId = courseId };
            db.CourseCompletionRules.Add(rule);
        }

        rule.RequiredModulePercent   = req.RequiredModulePercent;
        rule.RequireAllAssignments   = req.RequireAllAssignments;
        rule.RequireAllQuizzesPassed = req.RequireAllQuizzesPassed;
        rule.UpdatedAt               = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Ok(new CompletionRuleDto(
            rule.CourseId, rule.RequiredModulePercent,
            rule.RequireAllAssignments, rule.RequireAllQuizzesPassed, rule.UpdatedAt));
    }

    // ════════════════════════════════════════════════════════════════════════
    // COMPLETION STATUS — student cek status sendiri
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// GET /api/courses/{courseId}/completion-status
    /// Status completion real-time untuk user yang sedang login.
    /// Cocok untuk ditampilkan di sidebar course (progress bar + checklist).
    ///
    /// Response contoh:
    /// {
    ///   "modulePercent": 80.0,
    ///   "requiredModulePercent": 80,
    ///   "moduleCriteriaMet": true,
    ///   "assignmentCriteriaMet": true,
    ///   "quizCriteriaMet": false,
    ///   "isCompleted": false,
    ///   "hasCertificate": false,
    ///   "certificateNumber": null
    /// }
    /// </summary>
    [HttpGet("api/courses/{courseId:int}/completion-status")]
    public async Task<ActionResult<CompletionStatusDto>> GetStatus(int courseId)
    {
        var enrolled = await db.Enrollments
            .AnyAsync(e => e.CourseId == courseId && e.UserId == UserId);

        // Teacher/admin bisa cek status tanpa enroll
        if (!enrolled && UserRole == "student")
            return Forbid();

        var status = await completionService.GetStatusAsync(courseId, UserId);
        return Ok(status);
    }

    // ════════════════════════════════════════════════════════════════════════
    // CERTIFICATE — student claim dan lihat sertifikat
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// POST /api/courses/{courseId}/certificate/claim
    /// Student meminta sertifikat. Sistem cek otomatis apakah syarat terpenuhi.
    /// Idempotent: panggil berkali-kali aman, tidak buat duplikat.
    ///
    /// Response 200: sertifikat berhasil diterbitkan (atau sudah ada sebelumnya)
    /// Response 422: syarat belum terpenuhi, sertakan status detail
    /// </summary>
    [HttpPost("api/courses/{courseId:int}/certificate/claim")]
    public async Task<IActionResult> Claim(int courseId)
    {
        var enrolled = await db.Enrollments
            .AnyAsync(e => e.CourseId == courseId && e.UserId == UserId);
        if (!enrolled)
            return Forbid();

        var cert = await completionService.TryIssueCertificateAsync(courseId, UserId, UserName);
        if (cert != null)
        {
            var dto = await completionService.GetCertificateAsync(courseId, UserId);
            return Ok(dto);
        }

        // Belum memenuhi syarat — kembalikan status detail agar frontend bisa tampilkan
        var status = await completionService.GetStatusAsync(courseId, UserId);
        return UnprocessableEntity(new
        {
            message = "Persyaratan course belum terpenuhi.",
            status
        });
    }

    /// <summary>
    /// GET /api/courses/{courseId}/certificate
    /// Ambil sertifikat user untuk course ini. 404 jika belum dapat.
    /// </summary>
    [HttpGet("api/courses/{courseId:int}/certificate")]
    public async Task<ActionResult<CertificateDto>> GetMyCertificate(int courseId)
    {
        var cert = await completionService.GetCertificateAsync(courseId, UserId);
        if (cert == null)
            return NotFound(new { message = "Sertifikat belum diterbitkan untuk course ini." });

        return Ok(cert);
    }

    /// <summary>
    /// GET /api/certificates/me
    /// Semua sertifikat milik user yang sedang login.
    /// </summary>
    [HttpGet("api/certificates/me")]
    public async Task<ActionResult<IEnumerable<CertificateDto>>> GetMyCertificates()
    {
        var certs = await completionService.GetUserCertificatesAsync(UserId);
        return Ok(certs);
    }

    /// <summary>
    /// GET /api/certificates/verify/{number}
    /// Verifikasi keaslian sertifikat (endpoint publik, tidak perlu login).
    /// Digunakan untuk halaman verifikasi sertifikat yang bisa dibagikan ke siapa saja.
    /// </summary>
    [HttpGet("api/certificates/verify/{number}")]
    [AllowAnonymous]
    public async Task<IActionResult> Verify(string number)
    {
        var cert = await completionService.VerifyAsync(number);
        if (cert == null)
            return NotFound(new { valid = false, message = "Sertifikat tidak valid atau tidak ditemukan." });

        return Ok(new { valid = true, certificate = cert });
    }

    // ════════════════════════════════════════════════════════════════════════
    // ADMIN / INSTRUCTOR VIEW
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// GET /api/courses/{courseId}/certificates
    /// Semua sertifikat yang sudah diterbitkan untuk course ini.
    /// Hanya instructor atau admin.
    /// </summary>
    [HttpGet("api/courses/{courseId:int}/certificates")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> GetCourseCertificates(int courseId)
    {
        var course = await db.Courses.FindAsync(courseId);
        if (course == null) return NotFound();
        if (UserRole != "admin" && course.InstructorId != UserId) return Forbid();

        var certs = await db.Certificates
            .Include(c => c.Course)
            .Where(c => c.CourseId == courseId)
            .OrderByDescending(c => c.IssuedAt)
            .Select(c => new CertificateDto(
                c.Id, c.CourseId, c.Course.Title, c.Course.InstructorName,
                c.UserId, c.UserName, c.CertificateNumber, c.IssuedAt))
            .ToListAsync();

        return Ok(certs);
    }
}
