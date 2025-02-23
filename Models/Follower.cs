using Amazon.DynamoDBv2.DataModel;

namespace AnthemAPI.Models;

[DynamoDBTable("Followers")]
public class Follower
{
    [DynamoDBHashKey]
    [DynamoDBGlobalSecondaryIndexRangeKey("Follower-index")]
    public required string UserId { get; set; }
    [DynamoDBRangeKey]
    [DynamoDBGlobalSecondaryIndexHashKey("Follower-index")]
    public required string FollowerUserId { get; set; }
    public required DateTime CreatedAt { get; set; }
}

public class FollowerCard
{
    public required Card Card { get; set; }
    public required Follower Follower { get; set; }
}
