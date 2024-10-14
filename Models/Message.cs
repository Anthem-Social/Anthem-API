namespace AnthemAPI.Models;

public class Message
{
    public required string UserId { get; set; }
    public required DateTime Timestamp { get; set; }
    public required string Text { get; set; }
}