using System.ComponentModel.DataAnnotations;

namespace LmsApp.DTOs;

// ── Completion Rule ───────────────────────────────────────────────────────────

public record CompletionRuleDto(
    int CourseId,
    int RequiredModulePercent,
    bool RequireAllAssignments,
    bool RequireAllQuizzesPassed,
    bool RequireExamPassed,
    int? RequiredExamId,
    string? RequiredExamTitle,
    DateTime UpdatedAt
);

public record SetCompletionRuleRequest(
    [Range(0, 100)] int RequiredModulePercent = 100,
    bool RequireAllAssignments = false,
    bool RequireAllQuizzesPassed = false,
    bool RequireExamPassed = false,
    int? RequiredExamId = null
);

// ── Completion Status (student view) ─────────────────────────────────────────

public record CompletionStatusDto(
    double ModulePercent,          // persentase modul yang sudah selesai
    int RequiredModulePercent,     // minimal yang diharuskan
    bool ModuleCriteriaMet,
    bool AssignmentCriteriaMet,    // true jika tidak ada syarat assignment, atau sudah terpenuhi
    bool QuizCriteriaMet,
    bool ExamCriteriaMet,          // true jika tidak ada syarat ujian, atau sudah lulus
    string? RequiredExamTitle,     // judul ujian yang diwajibkan (null jika tidak ada)
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
