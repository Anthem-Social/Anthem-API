using Amazon.DynamoDBv2.DataModel;
using AnthemAPI.Common;

namespace AnthemAPI.Models;

/*
Since the Id here is used as the Sort key in the Feeds table,
we append the creator's UserId to the Timestamp to ensure the resulting Feed
record will be unique. Otherwise we run the risk of a user's two
friends posting at the exact same time.
*/

[DynamoDBTable("Posts")]
public class Post
{
    [DynamoDBHashKey]
    public required string UserId { get; set; }
    [DynamoDBRangeKey]
    public required string Id { get; set; } // ${CreatedAt:o}#{UserId}
    public string? Caption { get; set; }
    public required ContentType ContentType { get; set; }
    public required string Content { get; set; }
    public required long TotalLikes { get; set; }
    public required long TotalComments { get; set; }
}

public class PostCard
{
    public required Card Card { get; set; }
    public Like? Like { get; set; }
    public required Post Post { get; set; }
}

public class PostCreate
{
    public required ContentType ContentType { get; set; }
    public required string Content { get; set; }
    public string? Caption { get; set; }
}
