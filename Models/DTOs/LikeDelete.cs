namespace AnthemAPI.Models;

public class LikeDelete
{
    public required string PostId { get; set; }
    public required string Id { get; set; } // $"{CreatedAt:o}#{UserId}"
}
