namespace LmsApp.DTOs;

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
