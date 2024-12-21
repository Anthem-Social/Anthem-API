namespace AnthemAPI.Models;

public class Chat
{
    public required List<User> Users { get; set; }
    public required List<Message> Messages { get; set; }
}
