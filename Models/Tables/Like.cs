using Amazon.DynamoDBv2.DataModel;

namespace AnthemAPI.Models;

[DynamoDBTable("Likes")]
public class Like
{
    [DynamoDBHashKey]
    [DynamoDBGlobalSecondaryIndexRangeKey("UserId-index")]
    public required string PostId { get; set; }

    [DynamoDBRangeKey]
    public required string Id { get; set; } // $"{CreatedAt:o}#{UserId}"

    [DynamoDBGlobalSecondaryIndexHashKey("UserId-index")]
    public required string UserId { get; set; }
}
