using System.ComponentModel.DataAnnotations;

namespace LmsApp.DTOs;

// ── Completion Rule ───────────────────────────────────────────────────────────

public record CompletionRuleDto(
    int CourseId,
    int RequiredModulePercent,
    bool RequireAllAssignments,
    bool RequireAllQuizzesPassed,
    DateTime UpdatedAt
);

public record SetCompletionRuleRequest(
    [Range(1, 100)] int RequiredModulePercent = 100,
    bool RequireAllAssignments = false,
    bool RequireAllQuizzesPassed = false
);

// ── Completion Status (student view) ─────────────────────────────────────────

public record CompletionStatusDto(
    double ModulePercent,          // persentase modul yang sudah selesai
    int RequiredModulePercent,     // minimal yang diharuskan
    bool ModuleCriteriaMet,
    bool AssignmentCriteriaMet,    // true jika tidak ada syarat assignment, atau sudah terpenuhi
    bool QuizCriteriaMet,
    bool IsCompleted,              // semua kriteria terpenuhi
    bool HasCertificate,
    string? CertificateNumber
);

// ── Certificate ───────────────────────────────────────────────────────────────

public record CertificateDto(
    int Id,
    int CourseId,
    string CourseTitle,
    string InstructorName,
    string UserId,
    string UserName,
    string CertificateNumber,
    DateTime IssuedAt
);
