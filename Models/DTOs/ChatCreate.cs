namespace AnthemAPI.Models;

public class ChatCreate
{
    public required string Name { get; set; }
    public required HashSet<string> UserIds { get; set; }
    public required string CreatorUserId { get; set; }
}
