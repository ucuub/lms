using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Services;

public class CompletionService(LmsDbContext db, INotificationService notifications) : ICompletionService
{
    // ── Status ────────────────────────────────────────────────────────────────

    public async Task<CompletionStatusDto> GetStatusAsync(int courseId, string userId)
    {
        var rule = await GetRuleAsync(courseId);

        // 1. Cek progress modul
        var progress = await db.CourseProgresses
            .FirstOrDefaultAsync(p => p.CourseId == courseId && p.UserId == userId);

        var modulePercent = progress?.Percentage ?? 0;
        var moduleMet = modulePercent >= rule.RequiredModulePercent;

        // 2. Cek assignment (hanya jika rule mewajibkan)
        var assignmentMet = true;
        if (rule.RequireAllAssignments)
        {
            var totalAssignments = await db.Assignments
                .CountAsync(a => a.CourseId == courseId);

            if (totalAssignments > 0)
            {
                var submitted = await db.Submissions
                    .Where(s => s.UserId == userId)
                    .Join(db.Assignments.Where(a => a.CourseId == courseId),
                          s => s.AssignmentId, a => a.Id, (s, a) => s)
                    .Select(s => s.AssignmentId)
                    .Distinct()
                    .CountAsync();

                assignmentMet = submitted >= totalAssignments;
            }
        }

        // 3. Cek quiz (hanya jika rule mewajibkan)
        var quizMet = true;
        if (rule.RequireAllQuizzesPassed)
        {
            var quizIds = await db.Quizzes
                .Where(q => q.CourseId == courseId && q.IsPublished)
                .Select(q => q.Id)
                .ToListAsync();

            if (quizIds.Count > 0)
            {
                // Setiap quiz harus ada minimal satu attempt yang IsPassed = true
                var passedCount = await db.QuizAttempts
                    .Where(a => a.UserId == userId && a.IsPassed && quizIds.Contains(a.QuizId))
                    .Select(a => a.QuizId)
                    .Distinct()
                    .CountAsync();

                quizMet = passedCount >= quizIds.Count;
            }
        }

        // 4. Cek ujian (hanya jika rule mewajibkan)
        var examMet = true;
        string? requiredExamTitle = null;
        if (rule.RequireExamPassed && rule.RequiredExamId.HasValue)
        {
            var exam = await db.QuestionSets.FindAsync(rule.RequiredExamId.Value);
            requiredExamTitle = exam?.Title;

            examMet = await db.QuestionSetAttempts
                .AnyAsync(a => a.UserId == userId
                            && a.QuestionSetId == rule.RequiredExamId.Value
                            && a.IsPassed);
        }

        var isCompleted = moduleMet && assignmentMet && quizMet && examMet;

        // Cek apakah sudah punya sertifikat
        var cert = await db.Certificates
            .FirstOrDefaultAsync(c => c.CourseId == courseId && c.UserId == userId);

        return new CompletionStatusDto(
            ModulePercent: modulePercent,
            RequiredModulePercent: rule.RequiredModulePercent,
            ModuleCriteriaMet: moduleMet,
            AssignmentCriteriaMet: assignmentMet,
            QuizCriteriaMet: quizMet,
            ExamCriteriaMet: examMet,
            RequiredExamTitle: requiredExamTitle,
            IsCompleted: isCompleted,
            HasCertificate: cert != null,
            CertificateNumber: cert?.CertificateNumber
        );
    }

    // ── Certificate Issuance ──────────────────────────────────────────────────

    public async Task<Certificate?> TryIssueCertificateAsync(
        int courseId, string userId, string userName)
    {
        // Idempotency check — jangan buat duplikat
        var existing = await db.Certificates
            .FirstOrDefaultAsync(c => c.CourseId == courseId && c.UserId == userId);
        if (existing != null) return existing;

        // Cek apakah semua kriteria terpenuhi
        var status = await GetStatusAsync(courseId, userId);
        if (!status.IsCompleted) return null;

        // Generate certificate
        var cert = new Certificate
        {
            CourseId          = courseId,
            UserId            = userId,
            UserName          = userName,
            CertificateNumber = GenerateCertNumber(courseId),
            IssuedAt          = DateTime.UtcNow
        };
        db.Certificates.Add(cert);

        // Tandai enrollment sebagai Completed
        var enrollment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.CourseId == courseId && e.UserId == userId);
        if (enrollment != null)
        {
            enrollment.Status      = EnrollmentStatus.Completed;
            enrollment.CompletedAt = DateTime.UtcNow;
        }

        // Tandai CourseProgress.CompletedAt
        var progress = await db.CourseProgresses
            .FirstOrDefaultAsync(p => p.CourseId == courseId && p.UserId == userId);
        if (progress != null)
            progress.CompletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        // Kirim notifikasi ke student
        var course = await db.Courses.FindAsync(courseId);
        await notifications.CreateAsync(
            userId,
            "Sertifikat Diterima! 🎓",
            $"Selamat! Kamu telah menyelesaikan \"{course?.Title}\" dan mendapatkan sertifikat.",
            NotificationType.Success,
            $"/certificates/{cert.CertificateNumber}"
        );

        return cert;
    }

    // ── Query ─────────────────────────────────────────────────────────────────

    public async Task<CertificateDto?> GetCertificateAsync(int courseId, string userId)
    {
        var cert = await db.Certificates
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.CourseId == courseId && c.UserId == userId);

        return cert == null ? null : ToDto(cert);
    }

    public async Task<IEnumerable<CertificateDto>> GetUserCertificatesAsync(string userId)
    {
        var certs = await db.Certificates
            .Include(c => c.Course)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync();

        return certs.Select(ToDto);
    }

    public async Task<CertificateDto?> VerifyAsync(string certificateNumber)
    {
        var cert = await db.Certificates
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.CertificateNumber == certificateNumber);

        return cert == null ? null : ToDto(cert);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Ambil rule untuk course. Jika belum diset, kembalikan rule default
    /// (RequiredModulePercent=100, tidak ada syarat assignment/quiz).
    /// </summary>
    private async Task<CourseCompletionRule> GetRuleAsync(int courseId)
    {
        return await db.CourseCompletionRules
            .FirstOrDefaultAsync(r => r.CourseId == courseId)
            ?? new CourseCompletionRule
            {
                CourseId                = courseId,
                RequiredModulePercent   = 100,
                RequireAllAssignments   = false,
                RequireAllQuizzesPassed = false,
                RequireExamPassed       = false,
                RequiredExamId          = null,
            };
    }

    private static string GenerateCertNumber(int courseId)
    {
        var date   = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = Random.Shared.Next(100000, 999999);
        return $"LMS-{courseId:D4}-{date}-{random}";
    }

    private static CertificateDto ToDto(Certificate c) => new(
        c.Id, c.CourseId,
        c.Course?.Title         ?? string.Empty,
        c.Course?.InstructorName ?? string.Empty,
        c.UserId, c.UserName,
        c.CertificateNumber, c.IssuedAt
    );
}
