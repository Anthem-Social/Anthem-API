using Amazon.DynamoDBv2.DataModel;

namespace AnthemAPI.Models;

[DynamoDBTable("UserChats")]
public class UserChat
{
    [DynamoDBHashKey]
    public required string UserId { get; set; }
    [DynamoDBRangeKey]
    public string Id => $"{LastMessageAt}#{ChatId}";
    public required string ChatId { get; set; }
    public required DateTime LastMessageAt { get; set; }
    public required string Preview { get; set; }
}
