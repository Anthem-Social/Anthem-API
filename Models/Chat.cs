using Amazon.DynamoDBv2.DataModel;

namespace AnthemAPI.Models;

[DynamoDBTable("Chats")]
public class Chat
{
    [DynamoDBHashKey]
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required HashSet<string> UserIds { get; set; }
    public required DateTime LastMessageAt { get; set; }
    public required string Preview { get; set; }
    public required string CreatorUserId { get; set; }
    public required DateTime CreatedAt { get; set; }
}

public class ChatCreate
{
    public required string Name { get; set; }
    public required HashSet<string> UserIds { get; set; }
}
