namespace LmsApp.Models;

public class CourseProgress
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int CompletedModules { get; set; } = 0;
    public int TotalModules { get; set; } = 0;
    public double Percentage => TotalModules == 0 ? 0 : Math.Round((double)CompletedModules / TotalModules * 100, 1);
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public Course Course { get; set; } = null!;
    public ICollection<ModuleProgress> ModuleProgresses { get; set; } = [];
}

public class ModuleProgress
{
    public int Id { get; set; }
    public int CourseProgressId { get; set; }
    public int ModuleId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public CourseProgress CourseProgress { get; set; } = null!;
    public CourseModule Module { get; set; } = null!;
}
