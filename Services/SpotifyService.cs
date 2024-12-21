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
                return ServiceResult<string>.Failure($"Error response from fetching me: {response}", "SpotifyService.GetId()");
            }

            string content = await response.Content.ReadAsStringAsync();

            JsonElement json = JsonDocument.Parse(content).RootElement;

            string id = json.GetProperty("id").GetString()!;

            return ServiceResult<string>.Success(id);
        }
        catch (Exception e)
        {
            return ServiceResult<string>.Failure($"Failed to get id.\nError: {e}", "SpotifyService.GetId()");
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
                return ServiceResult<Status?>.Failure($"Error response from fetching me player: {response}", "SpotifyService.GetStatus()");
            }

            string content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(content))
            {
                return ServiceResult<Status?>.Success(null);
            }

            JsonElement json = JsonDocument.Parse(content).RootElement;

            string type = json.GetProperty("currently_playing_type").GetString()!;

            if (type != "track")
            {
                return ServiceResult<Status?>.Success(null);
            }

            JsonElement album = json.GetProperty("item").GetProperty("album");

            JsonElement artists = json.GetProperty("item").GetProperty("artists");

            var status = new Status
            {
                UserId = userId,
                Artists = artists.EnumerateArray()
                    .Select(artist => new Artist 
                    { 
                        Name = artist.GetProperty("name").GetString()!, 
                        Uri = artist.GetProperty("uri").GetString()! 
                    }).ToList(),
                Track = json.GetProperty("item").GetProperty("name").GetString()!,
                TrackUri = json.GetProperty("item").GetProperty("uri").GetString()!,
                AlbumCoverUrl = album.GetProperty("images")[1].GetProperty("url").GetString()!,
                AlbumUri = album.GetProperty("uri").GetString()!,
                LastChanged = json.GetProperty("timestamp").GetInt64()
            };

            return ServiceResult<Status?>.Success(status);
        }
        catch (Exception e)
        {
            return ServiceResult<Status?>.Failure($"Failed to get status.\n{e}", "SpotifyService.GetStatus()");
        }
    }
}
