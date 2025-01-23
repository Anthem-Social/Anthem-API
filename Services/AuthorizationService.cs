using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AnthemAPI.Common;
using AnthemAPI.Models;

namespace AnthemAPI.Services;

public class AuthorizationService
{
    private readonly DynamoDBContext _context;
    private readonly SpotifyService _spotifyService;

    public AuthorizationService(
        IAmazonDynamoDB db,
        SpotifyService spotifyService
    )
    {
        _spotifyService = spotifyService;
        _context = new DynamoDBContext(db);
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
            return ServiceResult<Authorization?>.Failure(e, $"Failed to load for {userId}.", "AuthorizationService.Load()");
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
            return ServiceResult<Authorization>.Failure(e, $"Failed to save for {authorization.UserId}.", "AuthorizationService.Save()");
        }
    }

    public async Task<ServiceResult<Authorization>> Save(JsonElement json)
    {
        try
        {
            string accessToken = json.GetProperty("access_token").GetString()!;
            string refreshToken = json.GetProperty("refresh_token").GetString()!;
            long expiresAt = json.GetProperty("expires_in").GetInt64() + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            var result = await _spotifyService.GetUserId(accessToken);

            if (result.Data is null || result.IsFailure)
            {
                return ServiceResult<Authorization>.Failure(null, "Failed to get userId.", "AuthorizationService.Save(json)");
            }

            string userId = result.Data!;
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
            return ServiceResult<Authorization>.Failure(e, "Failed to save.", "AuthorizationService.Save(json)");
        }
    }
}
