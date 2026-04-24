namespace LmsApp.Models;

public class Quiz
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TimeLimitMinutes { get; set; } = 30;
    public int MaxAttempts { get; set; } = 1;
    public int PassScore { get; set; } = 60;
    public DateTime? DueDate { get; set; }
    public bool IsPublished { get; set; } = false;
    public bool ShowAnswers { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Course Course { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = [];
    public ICollection<QuizAttempt> Attempts { get; set; } = [];
}
