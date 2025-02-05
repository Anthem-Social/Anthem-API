using Amazon.DynamoDBv2.DataModel;
using AnthemAPI.Common;

namespace AnthemAPI.Models;

[DynamoDBTable("Posts")]
public class Post
{
    [DynamoDBHashKey]
    public required string UserId { get; set; }
    [DynamoDBRangeKey]
    public required string Id { get; set; } // ${CreatedAt:o}#{UserId}
    public required ContentType ContentType { get; set; }
    public required string Content { get; set; }
    public string? Text { get; set; }
    public required long TotalLikes { get; set; }
    public required long TotalComments { get; set; }
}
