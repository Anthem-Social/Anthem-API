namespace AnthemAPI.Models;

public class Comment
{
    public required User User { get; set; }
    public required string PostId { get; set; }
    public required DateTime Timestamp { get; set; }
    public required string Text { get; set; }
}