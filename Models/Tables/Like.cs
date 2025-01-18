using Amazon.DynamoDBv2.DataModel;

namespace AnthemAPI.Models;

[DynamoDBTable("Likes")]
public class Like
{
    [DynamoDBHashKey]
    public required string PostId { get; set; }
    [DynamoDBRangeKey]
    public required string Id { get; set; } // $"{CreatedAt:o}#{UserId}"
    public required string UserId { get; set; }
    public required DateTime CreatedAt { get; set; }
}
