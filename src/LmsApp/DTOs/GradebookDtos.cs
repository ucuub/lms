namespace LmsApp.DTOs;

// ── Existing (kept for backward compat) ──────────────────────────────────────
public record GradebookStudentRow(
    string UserId,
    string UserName,
    string Email,
    List<GradeItem> Assignments,
    List<GradeItem> Quizzes,
    double? TotalPercentage,
    string LetterGrade
);

public record GradeItem(
    int Id,
    string Title,
    int? Score,
    int MaxScore,
    string Status // submitted|graded|not_submitted|pending
);

public record GradebookMyView(
    int CourseId,
    string CourseTitle,
    List<GradeItem> Assignments,
    List<GradeItem> Quizzes,
    double? TotalPercentage,
    string LetterGrade
);

// ── Grade Items (Weighted) ────────────────────────────────────────────────────
public record GradeItemConfigDto(
    int Id,
    int CourseId,
    int? AssignmentId,
    int? QuizId,
    string Name,
    string Type,          // Assignment | Quiz | Manual
    int MaxScore,
    double Weight,
    int Order,
    bool IsVisible,
    DateTime CreatedAt
);

public record CreateGradeItemRequest(
    string Name,
    string Type,          // "Assignment" | "Quiz" | "Manual"
    int MaxScore,
    double Weight,
    int Order,
    bool IsVisible,
    int? AssignmentId,
    int? QuizId
);

public record SetWeightRequest(double Weight);

public record SetManualGradeRequest(
    string UserId,
    string UserName,
    double Score,
    string? Comment
);

// ── Student view (weighted) ───────────────────────────────────────────────────
public record WeightedGradeItemView(
    int GradeItemId,
    string Name,
    string Type,
    double Weight,
    int MaxScore,
    double? Score,         // null = belum dinilai
    double? Percentage,    // Score/MaxScore*100
    string Status          // graded | not_submitted | manual
);

public record WeightedGradebookView(
    int CourseId,
    string CourseTitle,
    List<WeightedGradeItemView> Items,
    double? WeightedAverage,   // null jika belum ada nilai sama sekali
    string LetterGrade
);

// ── Teacher view (per student) ───────────────────────────────────────────────
public record WeightedStudentRow(
    string UserId,
    string UserName,
    List<WeightedGradeItemView> Items,
    double? WeightedAverage,
    string LetterGrade
);
