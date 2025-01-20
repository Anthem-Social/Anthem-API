using Amazon.DynamoDBv2.DataModel;
using AnthemAPI.Common;

namespace AnthemAPI.Models;

[DynamoDBTable("Messages")]
public class Message
{
    [DynamoDBHashKey]
    public required string ChatId { get; set; }
    [DynamoDBRangeKey]
    public required string Id { get; set; } // $"{CreatedAt:o}#{UserId}"
    public required ContentType ContentType { get; set; }
    public required string Content { get; set; }
}
