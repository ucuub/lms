using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Services;

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IAttendanceService
{
    // Sessions
    Task<IEnumerable<AttendanceSessionDto>> GetSessionsAsync(int courseId);
    Task<AttendanceSessionDetailDto?> GetSessionAsync(int sessionId, int courseId);
    Task<AttendanceSessionDto> CreateSessionAsync(int courseId, string requesterId, bool isAdmin, CreateSessionRequest req);
    Task DeleteSessionAsync(int sessionId, int courseId, string requesterId, bool isAdmin);

    // Records
    Task MarkAsync(int sessionId, int courseId, string requesterId, bool isAdmin, MarkAttendanceRequest req);
    Task BulkMarkAsync(int sessionId, int courseId, string requesterId, bool isAdmin, BulkMarkAttendanceRequest req);

    // Summary
    Task<IEnumerable<StudentAttendanceSummaryDto>> GetCourseSummaryAsync(int courseId, string requesterId, bool isAdmin);
    Task<StudentAttendanceSummaryDto?> GetStudentSummaryAsync(int courseId, string studentId);
}

// ── Implementation ────────────────────────────────────────────────────────────

public class AttendanceService(LmsDbContext db) : IAttendanceService
{
    // ── Sessions ──────────────────────────────────────────────────────────────

    public async Task<IEnumerable<AttendanceSessionDto>> GetSessionsAsync(int courseId)
    {
        var sessions = await db.AttendanceSessions
            .Include(s => s.Records)
            .Where(s => s.CourseId == courseId)
            .OrderByDescending(s => s.SessionDate)
            .ToListAsync();

        return sessions.Select(ToSessionDto);
    }

    public async Task<AttendanceSessionDetailDto?> GetSessionAsync(int sessionId, int courseId)
    {
        var session = await db.AttendanceSessions
            .Include(s => s.Records)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.CourseId == courseId);

        if (session == null) return null;

        return new AttendanceSessionDetailDto(
            session.Id, session.CourseId, session.Title, session.Description,
            session.SessionDate, session.CreatedAt,
            session.Records.OrderBy(r => r.UserName).Select(ToRecordDto)
        );
    }

    public async Task<AttendanceSessionDto> CreateSessionAsync(
        int courseId, string requesterId, bool isAdmin, CreateSessionRequest req)
    {
        await GuardTeacherAsync(courseId, requesterId, isAdmin);

        var session = new AttendanceSession
        {
            CourseId    = courseId,
            Title       = req.Title,
            Description = req.Description,
            SessionDate = req.SessionDate
        };
        db.AttendanceSessions.Add(session);
        await db.SaveChangesAsync();

        return ToSessionDto(session);
    }

    public async Task DeleteSessionAsync(int sessionId, int courseId, string requesterId, bool isAdmin)
    {
        await GuardTeacherAsync(courseId, requesterId, isAdmin);

        var session = await db.AttendanceSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.CourseId == courseId)
            ?? throw new KeyNotFoundException("Sesi tidak ditemukan.");

        db.AttendanceSessions.Remove(session);
        await db.SaveChangesAsync();
    }

    // ── Records ───────────────────────────────────────────────────────────────

    public async Task MarkAsync(
        int sessionId, int courseId,
        string requesterId, bool isAdmin,
        MarkAttendanceRequest req)
    {
        await GuardTeacherAsync(courseId, requesterId, isAdmin);

        var session = await db.AttendanceSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.CourseId == courseId)
            ?? throw new KeyNotFoundException("Sesi tidak ditemukan.");

        if (!Enum.TryParse<AttendanceStatus>(req.Status, ignoreCase: true, out var status))
            throw new InvalidOperationException($"Status tidak valid: {req.Status}. Gunakan Present, Absent, Late, atau Excused.");

        // Upsert: update jika sudah ada, insert jika belum
        var existing = await db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.SessionId == sessionId && r.UserId == req.UserId);

        if (existing != null)
        {
            existing.Status   = status;
            existing.Note     = req.Note;
            existing.MarkedAt = DateTime.UtcNow;
        }
        else
        {
            db.AttendanceRecords.Add(new AttendanceRecord
            {
                SessionId = sessionId,
                UserId    = req.UserId,
                UserName  = req.UserName,
                Status    = status,
                Note      = req.Note
            });
        }

        await db.SaveChangesAsync();
    }

    public async Task BulkMarkAsync(
        int sessionId, int courseId,
        string requesterId, bool isAdmin,
        BulkMarkAttendanceRequest req)
    {
        await GuardTeacherAsync(courseId, requesterId, isAdmin);

        var session = await db.AttendanceSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.CourseId == courseId)
            ?? throw new KeyNotFoundException("Sesi tidak ditemukan.");

        // Validate all statuses up front
        var parsed = req.Records.Select(r =>
        {
            if (!Enum.TryParse<AttendanceStatus>(r.Status, ignoreCase: true, out var s))
                throw new InvalidOperationException($"Status tidak valid: '{r.Status}' untuk user {r.UserId}.");
            return (r, s);
        }).ToList();

        // Load existing records for this session in one query
        var userIds  = parsed.Select(x => x.r.UserId).ToHashSet();
        var existing = await db.AttendanceRecords
            .Where(r => r.SessionId == sessionId && userIds.Contains(r.UserId))
            .ToDictionaryAsync(r => r.UserId);

        foreach (var (record, status) in parsed)
        {
            if (existing.TryGetValue(record.UserId, out var ex))
            {
                ex.Status   = status;
                ex.Note     = record.Note;
                ex.MarkedAt = DateTime.UtcNow;
            }
            else
            {
                db.AttendanceRecords.Add(new AttendanceRecord
                {
                    SessionId = sessionId,
                    UserId    = record.UserId,
                    UserName  = record.UserName,
                    Status    = status,
                    Note      = record.Note
                });
            }
        }

        await db.SaveChangesAsync();
    }

    // ── Summary ───────────────────────────────────────────────────────────────

    public async Task<IEnumerable<StudentAttendanceSummaryDto>> GetCourseSummaryAsync(
        int courseId, string requesterId, bool isAdmin)
    {
        await GuardTeacherAsync(courseId, requesterId, isAdmin);

        var totalSessions = await db.AttendanceSessions.CountAsync(s => s.CourseId == courseId);
        if (totalSessions == 0) return [];

        // Aggregate per student
        var records = await db.AttendanceRecords
            .Include(r => r.Session)
            .Where(r => r.Session.CourseId == courseId)
            .GroupBy(r => new { r.UserId, r.UserName })
            .Select(g => new StudentAttendanceSummaryDto(
                g.Key.UserId,
                g.Key.UserName,
                totalSessions,
                g.Count(r => r.Status == AttendanceStatus.Present),
                g.Count(r => r.Status == AttendanceStatus.Absent),
                g.Count(r => r.Status == AttendanceStatus.Late),
                g.Count(r => r.Status == AttendanceStatus.Excused),
                0.0  // placeholder — computed below
            ))
            .ToListAsync();

        // Compute attendance rate (EF Core can't compute this in the Select above)
        return records.Select(r => r with
        {
            AttendanceRate = totalSessions == 0
                ? 0.0
                : Math.Round((r.PresentCount + r.LateCount) / (double)totalSessions * 100, 1)
        });
    }

    public async Task<StudentAttendanceSummaryDto?> GetStudentSummaryAsync(int courseId, string studentId)
    {
        var totalSessions = await db.AttendanceSessions.CountAsync(s => s.CourseId == courseId);

        var records = await db.AttendanceRecords
            .Include(r => r.Session)
            .Where(r => r.Session.CourseId == courseId && r.UserId == studentId)
            .ToListAsync();

        if (records.Count == 0 && totalSessions == 0) return null;

        var userName = records.FirstOrDefault()?.UserName ?? studentId;

        var present  = records.Count(r => r.Status == AttendanceStatus.Present);
        var absent   = records.Count(r => r.Status == AttendanceStatus.Absent);
        var late     = records.Count(r => r.Status == AttendanceStatus.Late);
        var excused  = records.Count(r => r.Status == AttendanceStatus.Excused);
        var rate     = totalSessions == 0
            ? 0.0
            : Math.Round((present + late) / (double)totalSessions * 100, 1);

        return new StudentAttendanceSummaryDto(
            studentId, userName, totalSessions,
            present, absent, late, excused, rate);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task GuardTeacherAsync(int courseId, string userId, bool isAdmin)
    {
        if (isAdmin) return;
        var course = await db.Courses.FindAsync(courseId)
            ?? throw new KeyNotFoundException("Course tidak ditemukan.");
        if (course.InstructorId != userId)
            throw new UnauthorizedAccessException("Hanya instructor course ini yang bisa mengelola absensi.");
    }

    private static AttendanceSessionDto ToSessionDto(AttendanceSession s) => new(
        s.Id, s.CourseId, s.Title, s.Description, s.SessionDate,
        s.Records.Count,
        s.Records.Count(r => r.Status == AttendanceStatus.Present),
        s.Records.Count(r => r.Status == AttendanceStatus.Absent),
        s.Records.Count(r => r.Status == AttendanceStatus.Late),
        s.Records.Count(r => r.Status == AttendanceStatus.Excused),
        s.CreatedAt
    );

    private static AttendanceRecordDto ToRecordDto(AttendanceRecord r) => new(
        r.Id, r.SessionId, r.UserId, r.UserName, r.Status.ToString(), r.Note, r.MarkedAt
    );
}
