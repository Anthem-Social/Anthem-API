using Amazon.DynamoDBv2.DataModel;

namespace AnthemAPI.Models;

[DynamoDBTable("Messages")]
public class Message
{
    [DynamoDBHashKey]
    public required string ChatId { get; set; }
    [DynamoDBRangeKey]
    public string Id => $"{SentAt}#{UserId}";
    public required string UserId { get; set; }
    public required DateTime SentAt { get; set; }
    public required string Text { get; set; }
}
