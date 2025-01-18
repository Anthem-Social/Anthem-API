using Amazon.DynamoDBv2.DataModel;

namespace AnthemAPI.Models;

[DynamoDBTable("Follows")]
public class Follow
{
    [DynamoDBHashKey]
    [DynamoDBGlobalSecondaryIndexRangeKey("Follower-index")]
    public required string Followee { get; set; }
    [DynamoDBRangeKey]
    [DynamoDBGlobalSecondaryIndexHashKey("Follower-index")]
    public required string Follower { get; set; }
    public required DateTime CreatedAt { get; set; }
}
