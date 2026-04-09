using System.ComponentModel.DataAnnotations;

namespace LmsApp.DTOs;

// ── Requests ──────────────────────────────────────────────────────────────────

public record CreateSessionRequest(
    [Required, MaxLength(200)] string Title,
    string? Description,
    [Required] DateTime SessionDate
);

public record MarkAttendanceRequest(
    [Required] string UserId,
    [Required] string UserName,
    [Required] string Status,   // "Present" | "Absent" | "Late" | "Excused"
    string? Note
);

/// <summary>Batch mark untuk satu sesi — kirim semua student sekaligus.</summary>
public record BulkMarkAttendanceRequest(
    [Required] List<MarkAttendanceRequest> Records
);

// ── Responses ─────────────────────────────────────────────────────────────────

public record AttendanceSessionDto(
    int Id,
    int CourseId,
    string Title,
    string? Description,
    DateTime SessionDate,
    int TotalRecords,
    int PresentCount,
    int AbsentCount,
    int LateCount,
    int ExcusedCount,
    DateTime CreatedAt
);

public record AttendanceRecordDto(
    int Id,
    int SessionId,
    string UserId,
    string UserName,
    string Status,
    string? Note,
    DateTime MarkedAt
);

public record AttendanceSessionDetailDto(
    int Id,
    int CourseId,
    string Title,
    string? Description,
    DateTime SessionDate,
    DateTime CreatedAt,
    IEnumerable<AttendanceRecordDto> Records
);

/// <summary>Summary kehadiran satu student di semua sesi course.</summary>
public record StudentAttendanceSummaryDto(
    string UserId,
    string UserName,
    int TotalSessions,
    int PresentCount,
    int AbsentCount,
    int LateCount,
    int ExcusedCount,
    double AttendanceRate    // % (Present + Late) / Total
);
