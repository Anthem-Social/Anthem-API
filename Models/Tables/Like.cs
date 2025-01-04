using Amazon.DynamoDBv2.DataModel;

namespace AnthemAPI.Models;

[DynamoDBTable("Likes")]
public class Like
{
    [DynamoDBHashKey]
    public required string PostId { get; set; }
    [DynamoDBRangeKey]
    public required string UserId { get; set; }
    public required DateTime Timestamp { get; set; }
    [DynamoDBGlobalSecondaryIndexHashKey]
    public string UserIdIndex => UserId;
    [DynamoDBGlobalSecondaryIndexRangeKey]
    public string TimestampIndex => Timestamp.ToString("O");
}
