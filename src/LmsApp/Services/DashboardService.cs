using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Services;

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IDashboardService
{
    Task<StudentDashboardDto> GetStudentDashboardAsync(string userId);
    Task<TeacherDashboardDto> GetTeacherDashboardAsync(string userId);
}

// ── Implementation ────────────────────────────────────────────────────────────

public class DashboardService(LmsDbContext db) : IDashboardService
{
    // ── Student ───────────────────────────────────────────────────────────────

    public async Task<StudentDashboardDto> GetStudentDashboardAsync(string userId)
    {
        // 1. Enrollments + course info
        var enrollments = await db.Enrollments
            .Include(e => e.Course)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();

        var enrolledCourseIds = enrollments.Select(e => e.CourseId).ToHashSet();

        // 2. Progress per course
        var progressMap = await db.CourseProgresses
            .Where(p => p.UserId == userId && enrolledCourseIds.Contains(p.CourseId))
            .ToDictionaryAsync(p => p.CourseId);

        // 3. Certificates
        var certCourseIds = (await db.Certificates
            .Where(c => c.UserId == userId)
            .Select(c => c.CourseId)
            .ToListAsync()).ToHashSet();

        // 4. Upcoming deadlines (next 7 days)
        var upcomingDeadlines = await GetUpcomingDeadlinesAsync(
            userId, enrolledCourseIds);

        // 5. Recent activities (last 14 days)
        var recentActivities = await GetRecentActivitiesAsync(userId, enrolledCourseIds);

        // 6. Stats
        var totalSubmissions = await db.Submissions.CountAsync(s => s.UserId == userId);
        var stats = new StudentStatsDto(
            TotalEnrolled:     enrollments.Count,
            TotalCompleted:    enrollments.Count(e => e.Status == EnrollmentStatus.Completed),
            TotalCertificates: certCourseIds.Count,
            TotalSubmissions:  totalSubmissions
        );

        // 7. Build course progress list
        var courses = enrollments.Select(e =>
        {
            progressMap.TryGetValue(e.CourseId, out var p);
            var total     = p?.TotalModules    ?? 0;
            var completed = p?.CompletedModules ?? 0;
            var percent   = total == 0 ? 0.0 : Math.Round(completed / (double)total * 100, 1);

            return new EnrolledCourseProgressDto(
                e.CourseId,
                e.Course.Title,
                e.Course.ThumbnailUrl,
                e.Course.InstructorName,
                completed, total, percent,
                e.Status.ToString(),
                certCourseIds.Contains(e.CourseId),
                e.EnrolledAt,
                p?.CompletedAt
            );
        }).ToList();

        return new StudentDashboardDto(stats, courses, upcomingDeadlines, recentActivities);
    }

    // ── Teacher ───────────────────────────────────────────────────────────────

    public async Task<TeacherDashboardDto> GetTeacherDashboardAsync(string userId)
    {
        // 1. My courses (with enrollments + reviews for analytics)
        var courseIds = await db.Courses
            .Where(c => c.InstructorId == userId)
            .Select(c => c.Id)
            .ToListAsync();

        if (courseIds.Count == 0)
        {
            var emptyStats = new TeacherStatsDto(0, 0, 0, 0);
            return new TeacherDashboardDto(emptyStats, [], [], []);
        }

        // 2. Enrollment counts per course
        var enrollmentStats = await db.Enrollments
            .Where(e => courseIds.Contains(e.CourseId))
            .GroupBy(e => e.CourseId)
            .Select(g => new
            {
                CourseId    = g.Key,
                Total       = g.Count(),
                Completed   = g.Count(e => e.Status == EnrollmentStatus.Completed)
            })
            .ToDictionaryAsync(x => x.CourseId);

        // 3. Review stats per course
        var reviewStats = await db.CourseReviews
            .Where(r => courseIds.Contains(r.CourseId))
            .GroupBy(r => r.CourseId)
            .Select(g => new
            {
                CourseId = g.Key,
                Average  = g.Average(r => (double)r.Rating),
                Count    = g.Count()
            })
            .ToDictionaryAsync(x => x.CourseId);

        // 4. Pending grading count per course
        var pendingPerCourse = await db.Submissions
            .Include(s => s.Assignment)
            .Where(s => courseIds.Contains(s.Assignment.CourseId)
                     && s.Status == SubmissionStatus.Submitted)
            .GroupBy(s => s.Assignment.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourseId);

        // 5. Course list with analytics
        var courses = await db.Courses
            .Where(c => courseIds.Contains(c.Id))
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new { c.Id, c.Title, c.IsPublished, c.CreatedAt })
            .ToListAsync();

        var analytics = courses.Select(c =>
        {
            enrollmentStats.TryGetValue(c.Id, out var es);
            reviewStats.TryGetValue(c.Id, out var rs);
            pendingPerCourse.TryGetValue(c.Id, out var ps);

            var total      = es?.Total     ?? 0;
            var completed  = es?.Completed ?? 0;
            var completion = total == 0 ? 0.0 : Math.Round(completed / (double)total * 100, 1);

            return new CourseAnalyticsDto(
                c.Id, c.Title, c.IsPublished,
                total, completed, completion,
                ps?.Count ?? 0,
                rs != null ? Math.Round(rs.Average, 1) : 0.0,
                rs?.Count ?? 0,
                c.CreatedAt
            );
        }).ToList();

        // 6. Pending grading list (latest 20, sorted by oldest first so urgent on top)
        var pendingGrading = await db.Submissions
            .Include(s => s.Assignment).ThenInclude(a => a.Course)
            .Where(s => courseIds.Contains(s.Assignment.CourseId)
                     && s.Status == SubmissionStatus.Submitted)
            .OrderBy(s => s.SubmittedAt)
            .Take(20)
            .Select(s => new PendingGradingDto(
                s.Id,
                s.AssignmentId,
                s.Assignment.Title,
                s.Assignment.CourseId,
                s.Assignment.Course.Title,
                s.UserId,
                s.UserName,
                s.SubmittedAt
            ))
            .ToListAsync();

        // 7. Recent submissions (latest 10, all statuses)
        var recentSubmissions = await db.Submissions
            .Include(s => s.Assignment).ThenInclude(a => a.Course)
            .Where(s => courseIds.Contains(s.Assignment.CourseId))
            .OrderByDescending(s => s.SubmittedAt)
            .Take(10)
            .Select(s => new RecentSubmissionDto(
                s.Id,
                s.AssignmentId,
                s.Assignment.Title,
                s.Assignment.CourseId,
                s.Assignment.Course.Title,
                s.UserName,
                s.Status.ToString(),
                s.Score,
                s.Assignment.MaxScore,
                s.SubmittedAt
            ))
            .ToListAsync();

        // 8. Stats
        var uniqueStudents     = enrollmentStats.Values.Sum(e => e.Total);
        var totalPending       = pendingPerCourse.Values.Sum(p => p.Count);
        var todayStart         = DateTime.UtcNow.Date;
        var submissionsToday   = await db.Submissions
            .Include(s => s.Assignment)
            .CountAsync(s => courseIds.Contains(s.Assignment.CourseId)
                          && s.SubmittedAt >= todayStart);

        var stats = new TeacherStatsDto(
            TotalCourses:         courseIds.Count,
            TotalStudents:        uniqueStudents,
            TotalPendingGrading:  totalPending,
            TotalSubmissionsToday: submissionsToday
        );

        return new TeacherDashboardDto(stats, analytics, pendingGrading, recentSubmissions);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<List<UpcomingDeadlineDto>> GetUpcomingDeadlinesAsync(
        string userId, HashSet<int> enrolledCourseIds)
    {
        var now       = DateTime.UtcNow;
        var threshold = now.AddDays(7);

        // Assignments due in 7 days
        var assignments = await db.Assignments
            .Where(a => enrolledCourseIds.Contains(a.CourseId)
                     && a.DueDate != null
                     && a.DueDate > now
                     && a.DueDate <= threshold)
            .Select(a => new { a.Id, a.Title, a.CourseId, CourseTitle = a.Course.Title, DueDate = a.DueDate!.Value })
            .ToListAsync();

        // Which assignments did user already submit?
        var submittedIds = assignments.Count > 0
            ? (await db.Submissions
                .Where(s => s.UserId == userId
                         && assignments.Select(a => a.Id).Contains(s.AssignmentId))
                .Select(s => s.AssignmentId)
                .ToListAsync()).ToHashSet()
            : [];

        // Quizzes due in 7 days
        var quizzes = await db.Quizzes
            .Where(q => enrolledCourseIds.Contains(q.CourseId)
                     && q.IsPublished
                     && q.DueDate != null
                     && q.DueDate > now
                     && q.DueDate <= threshold)
            .Select(q => new { q.Id, q.Title, q.CourseId, CourseTitle = q.Course.Title, DueDate = q.DueDate!.Value })
            .ToListAsync();

        // Which quizzes did user already attempt?
        var attemptedIds = quizzes.Count > 0
            ? (await db.QuizAttempts
                .Where(a => a.UserId == userId
                         && quizzes.Select(q => q.Id).Contains(a.QuizId))
                .Select(a => a.QuizId)
                .Distinct()
                .ToListAsync()).ToHashSet()
            : [];

        var deadlines = assignments
            .Select(a => new UpcomingDeadlineDto(
                a.Id, "Assignment", a.Title, a.CourseId,
                a.CourseTitle, a.DueDate, submittedIds.Contains(a.Id)))
            .Concat(quizzes
                .Select(q => new UpcomingDeadlineDto(
                    q.Id, "Quiz", q.Title, q.CourseId,
                    q.CourseTitle, q.DueDate, attemptedIds.Contains(q.Id))))
            .OrderBy(d => d.DueDate)
            .ToList();

        return deadlines;
    }

    private async Task<List<RecentActivityDto>> GetRecentActivitiesAsync(
        string userId, HashSet<int> enrolledCourseIds)
    {
        var since = DateTime.UtcNow.AddDays(-14);
        var activities = new List<RecentActivityDto>();

        // Recent submissions
        var submissions = await db.Submissions
            .Include(s => s.Assignment).ThenInclude(a => a.Course)
            .Where(s => s.UserId == userId && s.SubmittedAt >= since)
            .OrderByDescending(s => s.SubmittedAt)
            .Take(5)
            .ToListAsync();

        activities.AddRange(submissions.Select(s => new RecentActivityDto(
            s.Status == SubmissionStatus.Graded ? "Graded" : "Submission",
            s.Status == SubmissionStatus.Graded
                ? $"Tugas dinilai: {s.Score}/{s.Assignment.MaxScore}"
                : $"Tugas dikumpulkan",
            s.Assignment.Title,
            s.Assignment.CourseId,
            s.Assignment.Course.Title,
            s.Status == SubmissionStatus.Graded && s.GradedAt.HasValue
                ? s.GradedAt.Value
                : s.SubmittedAt
        )));

        // Recent module completions
        var moduleProgress = await db.ModuleProgresses
            .Include(mp => mp.Module).ThenInclude(m => m.Course)
            .Where(mp => mp.UserId == userId && mp.CompletedAt >= since)
            .OrderByDescending(mp => mp.CompletedAt)
            .Take(5)
            .ToListAsync();

        activities.AddRange(moduleProgress.Select(mp => new RecentActivityDto(
            "ModuleCompleted",
            "Modul diselesaikan",
            mp.Module.Title,
            mp.Module.CourseId,
            mp.Module.Course.Title,
            mp.CompletedAt
        )));

        // Recent certificates
        var certificates = await db.Certificates
            .Include(c => c.Course)
            .Where(c => c.UserId == userId && c.IssuedAt >= since)
            .OrderByDescending(c => c.IssuedAt)
            .Take(3)
            .ToListAsync();

        activities.AddRange(certificates.Select(c => new RecentActivityDto(
            "Certificate",
            "Sertifikat diterima",
            c.Course.Title,
            c.CourseId,
            c.Course.Title,
            c.IssuedAt
        )));

        // Recent enrollments
        var enrollments = await db.Enrollments
            .Include(e => e.Course)
            .Where(e => e.UserId == userId && e.EnrolledAt >= since)
            .OrderByDescending(e => e.EnrolledAt)
            .Take(3)
            .ToListAsync();

        activities.AddRange(enrollments.Select(e => new RecentActivityDto(
            "Enrollment",
            "Mendaftar kursus baru",
            e.Course.Title,
            e.CourseId,
            e.Course.Title,
            e.EnrolledAt
        )));

        return activities
            .OrderByDescending(a => a.OccurredAt)
            .Take(15)
            .ToList();
    }
}
