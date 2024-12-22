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

    public async Task<ServiceResult<string>> GetId(string accessToken)
    {
        try
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await _client.GetAsync("me");

            if (!response.IsSuccessStatusCode)
            {
                return ServiceResult<string>.Failure(null, $"Error response: {response}", "SpotifyService.GetId()");
            }

            string content = await response.Content.ReadAsStringAsync();

            JsonElement json = JsonDocument.Parse(content).RootElement;

            string id = json.GetProperty("id").GetString()!;

            return ServiceResult<string>.Success(id);
        }
        catch (Exception e)
        {
            return ServiceResult<string>.Failure(e, "Failed to get id.", "SpotifyService.GetId()");
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

            // Create Album
            JsonElement albumJson = json.GetProperty("item").GetProperty("album"); 

            var album = new Album
            {
                Uri = albumJson.GetProperty("uri").GetString()!,
                CoverUrl = albumJson.GetProperty("images")[1].GetProperty("url").GetString()!
            };

            // Create list of Artists
            JsonElement artistsJson = json.GetProperty("item").GetProperty("artists");

            var artists = artistsJson
                .EnumerateArray()
                .Select(artist => new Artist 
                { 
                    Uri = artist.GetProperty("uri").GetString()!,
                    Name = artist.GetProperty("name").GetString()!
                }).ToList();

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
