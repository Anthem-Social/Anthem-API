using AnthemAPI.Common;

namespace AnthemAPI.Models;

public class MessageCreate
{
    public required string ChatId { get; set; }
    public required string UserId { get; set; }
    public required ContentType ContentType { get; set; }
    public required string Content { get; set; }
}
