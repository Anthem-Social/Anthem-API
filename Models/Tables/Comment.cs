using Amazon.DynamoDBv2.DataModel;

namespace AnthemAPI.Models;

[DynamoDBTable("Comments")]
public class Comment
{
    [DynamoDBHashKey]
    public required string PostId { get; set; }
    [DynamoDBRangeKey]
    public required string Id { get; set; } // $"{CreatedAt:o}#{UserId}"
    public required string Text { get; set; }
    public required string UserId { get; set; }
    public required DateTime CreatedAt { get; set; }
}
