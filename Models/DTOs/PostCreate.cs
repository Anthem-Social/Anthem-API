using AnthemAPI.Common;

namespace AnthemAPI.Models;

public class PostCreate
{
    public required ContentType ContentType { get; set; }
    public required string Content { get; set; }
    public string? Caption { get; set; }
}
