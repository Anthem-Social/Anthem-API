using Amazon.DynamoDBv2.DataModel;
using AnthemAPI.Common;

namespace AnthemAPI.Models;

[DynamoDBTable("Users")]
public class User
{
    [DynamoDBHashKey]
    public required string Id { get; set; }
    public required MusicProvider MusicProvider { get; set; }
    public string? Nickname { get; set; }
    public string? PictureUrl { get; set; }
    public string? Bio { get; set; }
    public Track? Anthem { get; set; }
    public required HashSet<string> ChatIds { get; set; }
}

public class UserUpdate
{
    public string? Nickname { get; set; }
    public string? PictureUrl { get; set; }
    public string? Bio { get; set; }
    public Track? Anthem { get; set; }
}
