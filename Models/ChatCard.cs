namespace AnthemAPI.Models;

public class ChatCard
{
    public required string ChatId { get; set; }
    public required string Name { get; set; }
    public required HashSet<string> UserIds { get; set; }
    public required string Preview { get; set; }
    public required DateTime LastMessageAt { get; set; }
    public required string CreatorUserId { get; set; }
    public required DateTime CreatedAt { get; set; }
}