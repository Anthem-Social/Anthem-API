using System.Text.Json;
using Amazon.DynamoDBv2.DataModel;
using AnthemAPI.Services;

namespace AnthemAPI.Models;

[DynamoDBTable("Authorizations")]
public class Authorization()
{
    [DynamoDBHashKey]
    public required string UserId { get; set; }
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public required long ExpiresAt { get; set; }

    public async Task<Authorization> FromJsonElement(JsonElement json, SpotifyService spotifyService)
    {
        string accessToken = json.GetProperty("access_token").GetString()!;
        string refreshToken = json.GetProperty("refresh_token").GetString()!;
        long expiresAt = json.GetProperty("expires_at").GetInt64();
        var result = await spotifyService.GetId(accessToken);
        string id = result.Data!;

        return new Authorization
        {
            UserId = id,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        };
    }
}
