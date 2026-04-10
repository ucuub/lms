using LmsApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace LmsApp.Controllers;

[ApiController]
[Route("api/courses/{courseId:int}")]
[Authorize(Roles = "teacher,admin")]
public class ReportsController(LmsDbContext db) : ControllerBase
{
    private string UserId   => User.FindFirst("sub")?.Value  ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";

    private async Task<bool> IsOwnerOrAdmin(int courseId)
    {
        if (UserRole == "admin") return true;
        var course = await db.Courses.FindAsync(courseId);
        return course?.InstructorId == UserId;
    }

    // GET /api/courses/{courseId}/attendance/export
    [HttpGet("attendance/export")]
    public async Task<IActionResult> ExportAttendance(int courseId)
    {
        if (!await IsOwnerOrAdmin(courseId)) return Forbid();

        var sessions = await db.AttendanceSessions
            .Include(s => s.Records)
            .Where(s => s.CourseId == courseId)
            .OrderBy(s => s.SessionDate)
            .ToListAsync();

        var enrollments = await db.Enrollments
            .Where(e => e.CourseId == courseId)
            .ToListAsync();

        var rows = enrollments.Select(e => new
        {
            UserId = e.UserId,
            StudentName = e.UserName,
            Sessions = sessions.Select(s => new
            {
                Session = s.Title,
                Date = s.SessionDate.ToString("yyyy-MM-dd"),
                Status = s.Records.FirstOrDefault(r => r.UserId == e.UserId)?.Status.ToString() ?? "Tidak Hadir"
            }).ToList()
        });

        var sb = new StringBuilder();
        // Header
        sb.Append("Student ID,Nama");
        foreach (var s in sessions) sb.Append($",{s.Title} ({s.SessionDate:yyyy-MM-dd})");
        sb.AppendLine();

        // Rows
        foreach (var row in rows)
        {
            sb.Append($"{row.UserId},{row.StudentName}");
            foreach (var s in sessions)
            {
                var rec = sessions.FirstOrDefault(ss => ss.Id == s.Id)?
                    .Records.FirstOrDefault(r => r.UserId == row.UserId);
                sb.Append($",{rec?.Status.ToString() ?? "Tidak Hadir"}");
            }
            sb.AppendLine();
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"attendance_course_{courseId}_{DateTime.Now:yyyyMMdd}.csv");
    }

    // GET /api/courses/{courseId}/completion/export
    [HttpGet("completion/export")]
    public async Task<IActionResult> ExportCompletion(int courseId)
    {
        if (!await IsOwnerOrAdmin(courseId)) return Forbid();

        var course = await db.Courses.FindAsync(courseId);
        if (course == null) return NotFound();

        var enrollments = await db.Enrollments
            .Where(e => e.CourseId == courseId)
            .ToListAsync();

        var progresses = await db.CourseProgresses
            .Where(p => p.CourseId == courseId)
            .ToListAsync();

        var certificates = await db.Certificates
            .Where(c => c.CourseId == courseId)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Student ID,Nama,Status Enroll,% Modul Selesai,Tanggal Selesai,Nomor Sertifikat");

        foreach (var e in enrollments)
        {
            var prog = progresses.FirstOrDefault(p => p.UserId == e.UserId);
            var cert = certificates.FirstOrDefault(c => c.UserId == e.UserId);
            var pct = prog != null && prog.TotalModules > 0
                ? $"{prog.CompletedModules * 100 / prog.TotalModules}%"
                : "0%";
            var completedAt = prog?.CompletedAt?.ToString("yyyy-MM-dd") ?? "-";
            var certNum = cert?.CertificateNumber ?? "-";
            sb.AppendLine($"{e.UserId},{e.UserName},{e.Status},{pct},{completedAt},{certNum}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"completion_course_{courseId}_{DateTime.Now:yyyyMMdd}.csv");
    }
}
