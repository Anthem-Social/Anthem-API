using Amazon.DynamoDBv2.DataModel;

namespace AnthemAPI.Models;

[DynamoDBTable("Comments")]
public class Comment
{
    [DynamoDBHashKey]
    public required string PostId { get; set; }
    [DynamoDBRangeKey]
    public required string CommentId { get; set; }
    public required string UserId { get; set; }
    public required string Text { get; set; }
    public required DateTime Timestamp { get; set; }
    [DynamoDBGlobalSecondaryIndexHashKey]
    public string UserIdIndex => UserId;
    [DynamoDBGlobalSecondaryIndexRangeKey]
    public string TimestampIndex => Timestamp.ToString("O");
}
