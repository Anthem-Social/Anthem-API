using System.Net.Mime;
using Amazon.DynamoDBv2.DataModel;

namespace AnthemAPI.Models;

[DynamoDBTable("Posts")]
public class Post
{
    [DynamoDBHashKey]
    public required string UserId { get; set; }
    [DynamoDBRangeKey]
    [DynamoDBGlobalSecondaryIndexHashKey("Id-index")]
    public required string Id { get; set; } // ${CreatedAt:o}#{UserId}
    public required ContentType ContentType { get; set; }
    public required string Content { get; set; }
    public required int TotalLikes { get; set; }
    public required int TotalComments { get; set; }
}
