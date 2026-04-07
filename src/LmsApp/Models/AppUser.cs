namespace LmsApp.Models;

public class AppUser
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;  // Keycloak 'sub' claim
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "student";       // student | teacher | admin (dikelola di LMS)
    public bool IsActive { get; set; } = true;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
}
