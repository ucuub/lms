namespace LmsApp.Models;

public class Assignment
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public int MaxScore { get; set; } = 100;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Course Course { get; set; } = null!;
    public ICollection<Submission> Submissions { get; set; } = [];
}
