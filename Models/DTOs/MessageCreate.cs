using AnthemAPI.Common;

namespace AnthemAPI.Models;

public class MessageCreate
{
    public required ContentType ContentType { get; set; }
    public required string Content { get; set; }
}
