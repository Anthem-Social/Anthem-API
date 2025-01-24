namespace AnthemAPI.Models;

public class CommentCreate
{
    public required string UserId { get; set; }
    public required string Text { get; set; }
}
