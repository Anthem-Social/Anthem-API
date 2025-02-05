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
}
