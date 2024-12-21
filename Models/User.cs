using Amazon.DynamoDBv2.DataModel;

namespace AnthemAPI.Models;

[DynamoDBTable("Users")]
public class User
{
    [DynamoDBHashKey("Id")]
    public required string Id { get; set; }

    [DynamoDBProperty("Followers")]
    public required HashSet<string> Followers { get; set; }

    [DynamoDBProperty("Following")]
    public required HashSet<string> Following { get; set; }

    [DynamoDBProperty("Friends")]
    public required HashSet<string> Friends { get; set; }
}
