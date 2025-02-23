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

public class LikeCard
{
    public required Card Card { get; set; }
    public required Like Like { get; set; }
}

public class LikeDelete
{
    public required string PostId { get; set; }
    public required string Id { get; set; }
}
