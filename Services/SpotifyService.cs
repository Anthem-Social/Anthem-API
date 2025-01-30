using System.Net.Http.Headers;
using System.Text.Json;
using AnthemAPI.Common;
using AnthemAPI.Models;

namespace AnthemAPI.Services;

public class SpotifyService
{
    private readonly HttpClient _client;

    public SpotifyService(HttpClient client)
    {
        _client = client;
        _client.BaseAddress = new Uri("https://api.spotify.com/v1/");
    }

    public async Task<ServiceResult<(string, MusicProvider)>> GetSubscriptionLevel(string accessToken)
    {
        try
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await _client.GetAsync("me");

            if (!response.IsSuccessStatusCode)
            {
                return ServiceResult<(string, MusicProvider)>.Failure(null, $"Error response: {response}", "SpotifyService.GetSubscriptionLevel()");
            }

            string content = await response.Content.ReadAsStringAsync();
            JsonElement json = JsonDocument.Parse(content).RootElement;
            string userId = json.GetProperty("id").GetString()!;
            string product = json.GetProperty("product").GetString()!;
            MusicProvider musicProvider = product == "premium"
                ? MusicProvider.SpotifyPremium
                : MusicProvider.SpotifyFree;

            return ServiceResult<(string, MusicProvider)>.Success((userId, musicProvider));
        }
        catch (Exception e)
        {
            return ServiceResult<(string, MusicProvider)>.Failure(e, "Failed to get subscription level.", "SpotifyService.GetSubscriptionLevel()");
        }
    }

    public async Task<ServiceResult<Status?>> GetStatus(string accessToken, string userId)
    {
        try
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await _client.GetAsync("me/player");

            if (!response.IsSuccessStatusCode)
            {
                return ServiceResult<Status?>.Failure(null, $"Error response: {response}", "SpotifyService.GetStatus()");
            }

            string content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(content))
            {
                return ServiceResult<Status?>.Success(null);
            }

            JsonElement json = JsonDocument.Parse(content).RootElement;

            // Ensure they are listening to a Track
            string type = json.GetProperty("currently_playing_type").GetString()!;

            if (type != "track")
            {
                return ServiceResult<Status?>.Success(null);
            }

            JsonElement albumJson = json.GetProperty("item").GetProperty("album"); 
            JsonElement artistsJson = json.GetProperty("item").GetProperty("artists");

            // Create Album
            var album = new Album
            {
                Uri = albumJson.GetProperty("uri").GetString()!,
                CoverUrl = albumJson.GetProperty("images")[1].GetProperty("url").GetString()!
            };

            // Create Artists
            var artists = artistsJson
                .EnumerateArray()
                .Select(artist => new Artist 
                { 
                    Uri = artist.GetProperty("uri").GetString()!,
                    Name = artist.GetProperty("name").GetString()!
                })
                .ToList();

            // Create Track
            var track = new Track
            {
                Uri = json.GetProperty("item").GetProperty("uri").GetString()!,
                Name = json.GetProperty("item").GetProperty("name").GetString()!,
                Artists = artists,
                Album = album
            };

            // Create Status
            var status = new Status
            {
                UserId = userId,
                Track = track,
                LastChanged = json.GetProperty("timestamp").GetInt64()
            };

            return ServiceResult<Status?>.Success(status);
        }
        catch (Exception e)
        {
            return ServiceResult<Status?>.Failure(e, $"Failed to get status for {userId}.", "SpotifyService.GetStatus()");
        }
    }
}
