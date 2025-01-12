using Amazon.DynamoDBv2.DataModel;

namespace AnthemAPI.Models;

public class Follow
{
    [DynamoDBHashKey]
    [DynamoDBGlobalSecondaryIndexRangeKey("FollowerUserId-index")]
    public required string FolloweeUserId { get; set; }
    [DynamoDBRangeKey]
    [DynamoDBGlobalSecondaryIndexHashKey("FollowerUserId-index")]
    public required string FollowerUserId { get; set; }
    public required DateTime FollowedAt { get; set; }
}
