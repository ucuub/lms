namespace LmsApp.DTOs;

// ═══════════════════════════════════════════════════════════════════
// STUDENT DASHBOARD
// ═══════════════════════════════════════════════════════════════════

public record StudentDashboardDto(
    StudentStatsDto Stats,
    IEnumerable<EnrolledCourseProgressDto> Courses,
    IEnumerable<UpcomingDeadlineDto> UpcomingDeadlines,
    IEnumerable<RecentActivityDto> RecentActivities,
    StudentQuizStatsDto QuizStats
);

public record StudentStatsDto(
    int TotalEnrolled,
    int TotalCompleted,
    int TotalCertificates,
    int TotalSubmissions
);

public record StudentQuizStatsDto(
    int TotalAttempts,
    double AvgScore,      // rata-rata persentase
    int PassedCount,
    int FailedCount
);

public record EnrolledCourseProgressDto(
    int CourseId,
    string Title,
    string? ThumbnailUrl,
    string InstructorName,
    int CompletedModules,
    int TotalModules,
    double ProgressPercent,
    string Status,          // "Active" | "Completed"
    bool HasCertificate,
    DateTime EnrolledAt,
    DateTime? CompletedAt
);

public record UpcomingDeadlineDto(
    int Id,
    string Type,            // "Assignment" | "Quiz"
    string Title,
    int CourseId,
    string CourseTitle,
    DateTime DueDate,
    bool IsSubmitted        // true = already submitted/attempted
);

public record RecentActivityDto(
    string Type,            // "Submission" | "ModuleCompleted" | "Certificate" | "Enrollment"
    string Title,
    string Description,
    int CourseId,
    string CourseTitle,
    DateTime OccurredAt
);

// ═══════════════════════════════════════════════════════════════════
// TEACHER DASHBOARD
// ═══════════════════════════════════════════════════════════════════

public record TeacherDashboardDto(
    TeacherStatsDto Stats,
    IEnumerable<CourseAnalyticsDto> Courses,
    IEnumerable<PendingGradingDto> PendingGrading,
    IEnumerable<RecentSubmissionDto> RecentSubmissions,
    IEnumerable<MonthlyEnrollmentDto> EnrollmentTrend
);

public record MonthlyEnrollmentDto(string Month, int Count);

public record TeacherStatsDto(
    int TotalCourses,
    int TotalStudents,      // unique students across all courses
    int TotalPendingGrading,
    int TotalSubmissionsToday
);

public record CourseAnalyticsDto(
    int CourseId,
    string Title,
    bool IsPublished,
    int EnrollmentCount,
    int CompletedCount,
    double CompletionRate,  // %
    int PendingGradingCount,
    double AverageRating,
    int ReviewCount,
    DateTime CreatedAt,
    double AvgQuizScore,    // rata-rata skor quiz peserta (%)
    int TotalQuizAttempts
);

public record PendingGradingDto(
    int SubmissionId,
    int AssignmentId,
    string AssignmentTitle,
    int CourseId,
    string CourseTitle,
    string StudentId,
    string StudentName,
    DateTime SubmittedAt
);

public record RecentSubmissionDto(
    int SubmissionId,
    int AssignmentId,
    string AssignmentTitle,
    int CourseId,
    string CourseTitle,
    string StudentName,
    string Status,          // "Submitted" | "Graded"
    int? Score,
    int? MaxScore,
    DateTime SubmittedAt
);
