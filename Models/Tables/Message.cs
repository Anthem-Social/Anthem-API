using Amazon.DynamoDBv2.DataModel;
using AnthemAPI.Common;

namespace AnthemAPI.Models;

[DynamoDBTable("Messages")]
public class Message
{
    [DynamoDBHashKey]
    public required string ChatId { get; set; }
    [DynamoDBRangeKey]
    public required string Id { get; set; } // Should be => $"{CreatedAt}#{UserId}"
    public required string UserId { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required ContentType ContentType { get; set; }
    public required string Content { get; set; }
}
