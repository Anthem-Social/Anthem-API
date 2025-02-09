using System.Net.Http.Headers;
using System.Text.Json;
using AnthemAPI.Common;
using AnthemAPI.Models;
using static AnthemAPI.Common.Utility;

namespace AnthemAPI.Services;

public class SpotifyService
{
    private readonly HttpClient _client;

    public SpotifyService(IHttpClientFactory factory)
    {
        _client = factory.CreateClient();
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

            Track track = GetTrack(json);

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

    public async Task<ServiceResult<List<object>>> Search(string accessToken, string query)
    {
        try
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await _client.GetAsync($"search?type=album%2Cartist%2Ctrack&q={query}");
            string content = await response.Content.ReadAsStringAsync();
            JsonElement json = JsonDocument.Parse(content).RootElement;
            List<JsonElement> items = json.GetProperty("items").EnumerateArray().ToList();
            var results = new List<object>();

            foreach (var item in items)
            {
                string type = item.GetProperty("type").GetString()!;
                
                if (type == "album")
                    results.Add(GetAlbum(item));
                else if (type == "artist")
                    results.Add(GetArtist(item));
                else if (type == "track")
                    results.Add(GetTrack(item));
            }

            return ServiceResult<List<object>>.Success(results);
        }
        catch (Exception e)
        {
            return ServiceResult<List<object>>.Failure(e, $"Query: {query}.", "SpotifyService.Search()");
        }
    }
}
