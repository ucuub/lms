using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Services;

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IPrerequisiteService
{
    Task<IEnumerable<PrerequisiteDto>> GetAsync(int courseId, string userId);
    Task<PrerequisiteCheckDto> CheckAsync(int courseId, string userId);
    Task<PrerequisiteDto> AddAsync(int courseId, int prerequisiteCourseId, string requesterId, bool isAdmin);
    Task RemoveAsync(int prerequisiteId, int courseId, string requesterId, bool isAdmin);
}

// ── Implementation ────────────────────────────────────────────────────────────

public class PrerequisiteService(LmsDbContext db) : IPrerequisiteService
{
    public async Task<IEnumerable<PrerequisiteDto>> GetAsync(int courseId, string userId)
    {
        var prereqs = await db.CoursePrerequisites
            .Include(p => p.PrerequisiteCourse)
            .Where(p => p.CourseId == courseId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        // Courses yang sudah diselesaikan user
        var completedIds = await GetCompletedCourseIdsAsync(userId);

        return prereqs.Select(p => new PrerequisiteDto(
            p.Id,
            p.PrerequisiteCourseId,
            p.PrerequisiteCourse.Title,
            p.PrerequisiteCourse.Level,
            p.PrerequisiteCourse.ThumbnailUrl,
            completedIds.Contains(p.PrerequisiteCourseId)
        ));
    }

    public async Task<PrerequisiteCheckDto> CheckAsync(int courseId, string userId)
    {
        var prereqs = await GetAsync(courseId, userId);
        var list    = prereqs.ToList();
        var canEnroll = list.Count == 0 || list.All(p => p.IsMet);
        return new PrerequisiteCheckDto(canEnroll, list);
    }

    public async Task<PrerequisiteDto> AddAsync(
        int courseId, int prerequisiteCourseId,
        string requesterId, bool isAdmin)
    {
        var course = await db.Courses.FindAsync(courseId)
            ?? throw new KeyNotFoundException("Course tidak ditemukan.");

        if (!isAdmin && course.InstructorId != requesterId)
            throw new UnauthorizedAccessException("Hanya instructor course ini yang bisa mengelola prerequisite.");

        // Jangan tambah prerequisite ke diri sendiri
        if (courseId == prerequisiteCourseId)
            throw new InvalidOperationException("Course tidak bisa menjadi prerequisite untuk dirinya sendiri.");

        var prereqCourse = await db.Courses.FindAsync(prerequisiteCourseId)
            ?? throw new KeyNotFoundException("Prerequisite course tidak ditemukan.");

        // Cek duplikat
        var exists = await db.CoursePrerequisites.AnyAsync(p =>
            p.CourseId == courseId && p.PrerequisiteCourseId == prerequisiteCourseId);
        if (exists)
            throw new InvalidOperationException("Prerequisite ini sudah terdaftar.");

        // Cek circular: apakah prerequisiteCourse sudah punya courseId sebagai prerequisitenya?
        var wouldCycle = await HasCircularDependencyAsync(prerequisiteCourseId, courseId);
        if (wouldCycle)
            throw new InvalidOperationException(
                "Tidak bisa menambah prerequisite ini karena akan membuat dependensi melingkar (circular dependency).");

        var entity = new CoursePrerequisite
        {
            CourseId             = courseId,
            PrerequisiteCourseId = prerequisiteCourseId
        };
        db.CoursePrerequisites.Add(entity);
        await db.SaveChangesAsync();

        return new PrerequisiteDto(
            entity.Id,
            prereqCourse.Id,
            prereqCourse.Title,
            prereqCourse.Level,
            prereqCourse.ThumbnailUrl,
            false   // baru ditambahkan, belum ada konteks user — caller tentukan
        );
    }

    public async Task RemoveAsync(int prerequisiteId, int courseId, string requesterId, bool isAdmin)
    {
        var entity = await db.CoursePrerequisites
            .Include(p => p.Course)
            .FirstOrDefaultAsync(p => p.Id == prerequisiteId && p.CourseId == courseId)
            ?? throw new KeyNotFoundException("Prerequisite tidak ditemukan.");

        if (!isAdmin && entity.Course.InstructorId != requesterId)
            throw new UnauthorizedAccessException("Hanya instructor course ini yang bisa mengelola prerequisite.");

        db.CoursePrerequisites.Remove(entity);
        await db.SaveChangesAsync();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Course IDs yang sudah diselesaikan user berdasarkan Enrollment.Status = Completed.
    /// </summary>
    private async Task<HashSet<int>> GetCompletedCourseIdsAsync(string userId)
    {
        var ids = await db.Enrollments
            .Where(e => e.UserId == userId && e.Status == EnrollmentStatus.Completed)
            .Select(e => e.CourseId)
            .ToListAsync();
        return ids.ToHashSet();
    }

    /// <summary>
    /// Deteksi circular dependency: apakah targetCourseId sudah (langsung atau transitif)
    /// memerlukan sourceCourseId sebagai prerequisite?
    /// BFS sederhana — cukup untuk mencegah cycle di graph prerequisite.
    /// </summary>
    private async Task<bool> HasCircularDependencyAsync(int sourceCourseId, int targetCourseId)
    {
        // BFS: mulai dari sourceCourseId, telusuri semua prerequisitenya
        // Jika targetCourseId ditemukan → ada cycle
        var visited = new HashSet<int>();
        var queue   = new Queue<int>();
        queue.Enqueue(sourceCourseId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current)) continue;

            var prereqIds = await db.CoursePrerequisites
                .Where(p => p.CourseId == current)
                .Select(p => p.PrerequisiteCourseId)
                .ToListAsync();

            foreach (var pid in prereqIds)
            {
                if (pid == targetCourseId) return true;
                queue.Enqueue(pid);
            }
        }

        return false;
    }
}
