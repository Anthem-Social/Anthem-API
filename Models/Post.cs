namespace AnthemAPI.Models;

public class Post
{
    public required string PostId { get; set; }
    public required User User { get; set; }
    public required Resource Resource { get; set; }
    public required DateTime Timestamp { get; set; }
    public required List<Like> Likes { get; set; }
    public required List<Comment> Comments { get; set; }
}