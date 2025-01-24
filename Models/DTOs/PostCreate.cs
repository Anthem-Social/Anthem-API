using AnthemAPI.Common;

namespace AnthemAPI.Models;

public class PostCreate
{
    public required string UserId { get; set; }
    public required ContentType ContentType { get; set; }
    public required string Content { get; set; }
    public string? Text { get; set; }
}
