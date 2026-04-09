using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

/// <summary>
/// Attendance Tracking — absensi per sesi per course.
///
/// Routes (teacher/admin):
///   GET    api/courses/{courseId}/attendance/sessions              → list sesi
///   GET    api/courses/{courseId}/attendance/sessions/{id}         → detail sesi + records
///   POST   api/courses/{courseId}/attendance/sessions              → buat sesi baru
///   DELETE api/courses/{courseId}/attendance/sessions/{id}         → hapus sesi
///   POST   api/courses/{courseId}/attendance/sessions/{id}/mark    → mark satu student
///   POST   api/courses/{courseId}/attendance/sessions/{id}/bulk    → mark semua student sekaligus
///   GET    api/courses/{courseId}/attendance/summary               → summary semua student
///
/// Routes (student):
///   GET    api/courses/{courseId}/attendance/me                    → summary kehadiran sendiri
/// </summary>
[ApiController]
[Route("api/courses/{courseId:int}/attendance")]
[Authorize]
public class AttendanceController(LmsDbContext db, IAttendanceService attendanceService) : ControllerBase
{
    private string UserId   => User.FindFirst("sub")?.Value  ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";
    private bool   IsAdmin  => UserRole == "admin";

    // ── Sessions ──────────────────────────────────────────────────────────────

    [HttpGet("sessions")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<IEnumerable<AttendanceSessionDto>>> GetSessions(int courseId)
    {
        try
        {
            var result = await attendanceService.GetSessionsAsync(courseId);
            return Ok(result);
        }
        catch (KeyNotFoundException e) { return NotFound(new { message = e.Message }); }
    }

    [HttpGet("sessions/{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<AttendanceSessionDetailDto>> GetSession(int courseId, int id)
    {
        var result = await attendanceService.GetSessionAsync(id, courseId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("sessions")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<AttendanceSessionDto>> CreateSession(
        int courseId, CreateSessionRequest req)
    {
        try
        {
            var result = await attendanceService.CreateSessionAsync(courseId, UserId, IsAdmin, req);
            return CreatedAtAction(nameof(GetSession), new { courseId, id = result.Id }, result);
        }
        catch (KeyNotFoundException e)      { return NotFound(new { message = e.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpDelete("sessions/{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> DeleteSession(int courseId, int id)
    {
        try
        {
            await attendanceService.DeleteSessionAsync(id, courseId, UserId, IsAdmin);
            return NoContent();
        }
        catch (KeyNotFoundException e)      { return NotFound(new { message = e.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // ── Mark Attendance ───────────────────────────────────────────────────────

    /// <summary>Mark kehadiran satu student di satu sesi. Upsert — aman dipanggil ulang.</summary>
    [HttpPost("sessions/{id:int}/mark")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> Mark(int courseId, int id, MarkAttendanceRequest req)
    {
        try
        {
            await attendanceService.MarkAsync(id, courseId, UserId, IsAdmin, req);
            return NoContent();
        }
        catch (KeyNotFoundException e)      { return NotFound(new { message = e.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException e) { return BadRequest(new { message = e.Message }); }
    }

    /// <summary>
    /// Bulk mark semua student dalam satu request.
    /// Biasanya dipanggil setelah form absensi diisi, submit sekali untuk seluruh kelas.
    /// </summary>
    [HttpPost("sessions/{id:int}/bulk")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> BulkMark(int courseId, int id, BulkMarkAttendanceRequest req)
    {
        try
        {
            await attendanceService.BulkMarkAsync(id, courseId, UserId, IsAdmin, req);
            return NoContent();
        }
        catch (KeyNotFoundException e)      { return NotFound(new { message = e.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException e) { return BadRequest(new { message = e.Message }); }
    }

    // ── Summary ───────────────────────────────────────────────────────────────

    /// <summary>Summary kehadiran semua student di course. Teacher/admin only.</summary>
    [HttpGet("summary")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<IEnumerable<StudentAttendanceSummaryDto>>> GetSummary(int courseId)
    {
        try
        {
            var result = await attendanceService.GetCourseSummaryAsync(courseId, UserId, IsAdmin);
            return Ok(result);
        }
        catch (KeyNotFoundException e)      { return NotFound(new { message = e.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    /// <summary>Student melihat rekap kehadirannya sendiri.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<StudentAttendanceSummaryDto>> GetMyAttendance(int courseId)
    {
        // Pastikan user enrolled atau teacher
        if (UserRole == "student")
        {
            var enrolled = await db.Enrollments
                .AnyAsync(e => e.CourseId == courseId && e.UserId == UserId);
            if (!enrolled) return Forbid();
        }

        var result = await attendanceService.GetStudentSummaryAsync(courseId, UserId);
        if (result == null) return Ok(new StudentAttendanceSummaryDto(UserId, string.Empty, 0, 0, 0, 0, 0, 0.0));
        return Ok(result);
    }
}
