using LmsApp.Models;

namespace LmsApp.ViewModels;

public class GradebookViewModel
{
    public Course Course { get; set; } = null!;
    public IList<Enrollment> Enrollments { get; set; } = [];
    public IList<Assignment> Assignments { get; set; } = [];
    public IList<Quiz> Quizzes { get; set; } = [];
    public IList<Submission> Submissions { get; set; } = [];
    public IList<QuizAttempt> QuizAttempts { get; set; } = [];

    public Submission? GetSubmission(int assignmentId, string userId) =>
        Submissions.FirstOrDefault(s => s.AssignmentId == assignmentId && s.UserId == userId);

    public QuizAttempt? GetBestAttempt(int quizId, string userId) =>
        QuizAttempts.Where(a => a.QuizId == quizId && a.UserId == userId)
                    .OrderByDescending(a => a.Score).FirstOrDefault();
}

public class MyGradebookViewModel
{
    public Course Course { get; set; } = null!;
    public IList<Assignment> Assignments { get; set; } = [];
    public IList<Quiz> Quizzes { get; set; } = [];
    public string UserId { get; set; } = string.Empty;
}
