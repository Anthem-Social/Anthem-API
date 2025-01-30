using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AnthemAPI.Common;
using AnthemAPI.Models;

namespace AnthemAPI.Services;

public class AuthorizationsService
{
    private readonly DynamoDBContext _context;
    private readonly SpotifyService _spotifyService;

    public AuthorizationsService(
        IAmazonDynamoDB client,
        SpotifyService spotifyService
    )
    {
        _spotifyService = spotifyService;
        _context = new DynamoDBContext(client);
    }

    public async Task<ServiceResult<Authorization?>> Load(string userId)
    {
        try
        {
            Authorization? authorization = await _context.LoadAsync<Authorization>(userId);
            return ServiceResult<Authorization?>.Success(authorization);
        }
        catch (Exception e)
        {
            return ServiceResult<Authorization?>.Failure(e, $"Failed to load for {userId}.", "AuthorizationsService.Load()");
        }
    }

    public async Task<ServiceResult<Authorization>> Save(Authorization authorization)
    {
        try
        {
            await _context.SaveAsync(authorization);
            return ServiceResult<Authorization>.Success(authorization);
        }
        catch (Exception e)
        {
            return ServiceResult<Authorization>.Failure(e, $"Failed to save for {authorization.UserId}.", "AuthorizationsService.Save()");
        }
    }

    public async Task<ServiceResult<Authorization>> Save(string userId, JsonElement json)
    {
        try
        {
            string accessToken = json.GetProperty("access_token").GetString()!;
            string refreshToken = json.GetProperty("refresh_token").GetString()!;
            long expiresAt = json.GetProperty("expires_in").GetInt64() + DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var authorization = new Authorization
            {
                UserId = userId,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt
            };

            return await Save(authorization);
        }
        catch (Exception e)
        {
            return ServiceResult<Authorization>.Failure(e, "Failed to save.", "AuthorizationsService.Save(json)");
        }
    }
}
