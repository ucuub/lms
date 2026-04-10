namespace LmsApp.Models;

public class Conversation
{
    public int Id { get; set; }
    public string User1Id { get; set; } = string.Empty;
    public string User1Name { get; set; } = string.Empty;
    public string User2Id { get; set; } = string.Empty;
    public string User2Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Message> Messages { get; set; } = [];
}
