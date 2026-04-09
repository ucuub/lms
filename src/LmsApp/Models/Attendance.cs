namespace LmsApp.Models;

/// <summary>
/// Satu record kehadiran per student per sesi.
/// Teacher membuat sesi (AttendanceSession), lalu mark kehadiran per student.
/// </summary>
public class AttendanceSession
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public string Title { get; set; } = string.Empty;   // "Pertemuan 1", "Week 3", dsb
    public string? Description { get; set; }
    public DateTime SessionDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<AttendanceRecord> Records { get; set; } = [];
}

public class AttendanceRecord
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public AttendanceSession Session { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public string? Note { get; set; }
    public DateTime MarkedAt { get; set; } = DateTime.UtcNow;
}

public enum AttendanceStatus
{
    Present,    // Hadir
    Absent,     // Tidak hadir
    Late,       // Terlambat
    Excused     // Izin
}
