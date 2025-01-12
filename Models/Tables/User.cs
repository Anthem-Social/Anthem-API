using Amazon.DynamoDBv2.DataModel;

namespace AnthemAPI.Models;

[DynamoDBTable("Users")]
public class User
{
    [DynamoDBHashKey]
    public required string Id { get; set; }
    public required bool Premium { get; set; }
    public required string Nickname { get; set; }
    public string? PictureUrl { get; set; }
    public string? Bio { get; set ; }
    public Track? Anthem { get; set; }
    public required HashSet<string> ChatIds {get; set; }
}
