using Amazon.DynamoDBv2.DataModel;

namespace AnthemAPI.Models;

[DynamoDBTable("Users")]
public class User
{
    [DynamoDBHashKey]
    public required string Id { get; set; }
    public string? Nickname { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set ; }
    public Track? Anthem { get; set; }
    public required HashSet<string> Followers { get; set; }
    public required HashSet<string> Following { get; set; }
    public required HashSet<string> Friends { get; set; }
}
