using Amazon.DynamoDBv2.DataModel;

namespace AnthemAPI.Models;

[DynamoDBTable("Authorizations")]
public class Authorization()
{
    [DynamoDBHashKey]
    public required string UserId { get; set; }
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public required long ExpiresAt { get; set; }
}
