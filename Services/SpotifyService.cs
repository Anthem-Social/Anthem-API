using System.Net.Http.Headers;
using System.Text.Json;
using AnthemAPI.Common;
using AnthemAPI.Models;
using static AnthemAPI.Common.Utility;
using static AnthemAPI.Common.Constants;

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

            JsonElement item = json.GetProperty("item");

            Track track = GetTrack(item);

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

    public async Task<ServiceResult<List<SearchResult>>> Search(string accessToken, string query, string type)
    {
        try
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await _client.GetAsync($"search?limit={SPOTIFY_SEARCH_ITEMS_LIMIT}&q={query}&type={type}");

            if (!response.IsSuccessStatusCode)
            {
                return ServiceResult<List<SearchResult>>.Failure(null, $"Error response: {response}", "SpotifyService.Search()");
            }

            string content = await response.Content.ReadAsStringAsync();
            
            JsonElement json = JsonDocument.Parse(content).RootElement;
            var results = new List<SearchResult>();

            // Check for Album results
            if (json.TryGetProperty("albums", out JsonElement albums))
            {
                albums
                    .GetProperty("items")
                    .EnumerateArray()
                    .ToList()
                    .ForEach(album => results.Add(new SearchResult(GetAlbum(album))));
            }

            // Check for Artist results
            if (json.TryGetProperty("artists", out JsonElement artists))
            {
                artists
                    .GetProperty("items")
                    .EnumerateArray()
                    .ToList()
                    .ForEach(artist => results.Add(new SearchResult(GetArtist(artist))));
            }

            // Check for Track results
            if (json.TryGetProperty("tracks", out JsonElement tracks))
            {
                tracks
                    .GetProperty("items")
                    .EnumerateArray()
                    .ToList()
                    .ForEach(track => results.Add(new SearchResult(GetTrack(track))));
            }

            return ServiceResult<List<SearchResult>>.Success(results);
        }
        catch (Exception e)
        {
            return ServiceResult<List<SearchResult>>.Failure(e, $"Query: {query}.", "SpotifyService.Search()");
        }
    }
}
