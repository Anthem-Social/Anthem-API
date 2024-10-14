namespace AnthemAPI.Models;

public class Like
{
    public required User User { get; set; }
    public required string PostId { get; set; }
    public required DateTime Timestamp { get; set; }
}