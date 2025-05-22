using System.Net.Http.Headers;
using System.Text.Json;
using AnthemAPI.Common;
using AnthemAPI.Models;
using static AnthemAPI.Common.Utility;
using static AnthemAPI.Common.Constants;
using HtmlAgilityPack;

namespace AnthemAPI.Services;

public class SpotifyService
{
    private readonly HttpClient _client;
    private readonly HttpClient _previewUrlClient;

    public SpotifyService(IHttpClientFactory factory)
    {
        _client = factory.CreateClient();
        _client.BaseAddress = new Uri("https://api.spotify.com/v1/");
        _previewUrlClient = factory.CreateClient();
        _previewUrlClient.BaseAddress = new Uri("https://open.spotify.com/track/");
    }

    public async Task<ServiceResult<Account>> GetAccount(string accessToken)
    {
        try
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await _client.GetAsync("me");

            if (!response.IsSuccessStatusCode)
                return ServiceResult<Account>.Failure(null, $"Error response: {response}", "SpotifyService.GetSubscriptionLevel()");

            string content = await response.Content.ReadAsStringAsync();
            JsonElement json = JsonDocument.Parse(content).RootElement;
            string userId = json.GetProperty("id").GetString()!;
            string product = json.GetProperty("product").GetString()!;
            MusicProvider musicProvider = product == "premium"
                ? MusicProvider.SpotifyPremium
                : MusicProvider.SpotifyFree;
            
            string? pictureUrl = null;
            if (json.TryGetProperty("images", out JsonElement images))
            {
                foreach (JsonElement image in images.EnumerateArray())
                {
                    if (image.GetProperty("height").GetInt32() == 300 && image.GetProperty("width").GetInt32() == 300)
                    {
                        pictureUrl = image.GetProperty("url").GetString();
                        break;
                    }
                }
            }

            var account = new Account
            {
                Id = userId,
                MusicProvider = musicProvider,
                PictureUrl = pictureUrl
            };

            return ServiceResult<Account>.Success(account);
        }
        catch (Exception e)
        {
            return ServiceResult<Account>.Failure(e, "Failed to get account.", "SpotifyService.GetAccount()");
        }
    }

    public async Task<ServiceResult<string?>> GetAnthemQueuePlaylistId(string accessToken, string userId)
    {
        try
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            string content;
            JsonElement json;
            int limit = 50;
            int offset = 0;
            List<(string, string)> playlists;
            HttpResponseMessage response;

            // Try to find the 'Anthem Queue' playlist
            do
            {
                response = await _client.GetAsync($"me/playlists?limit={limit}&offset={offset}");

                if (!response.IsSuccessStatusCode)
                    return ServiceResult<string?>.Failure(null, $"Error response: {response}", "SpotifyService.GetFeedPlaylistUri()");

                content = await response.Content.ReadAsStringAsync();
                json = JsonDocument.Parse(content).RootElement;

                playlists = json.GetProperty("items")
                    .EnumerateArray()
                    .Select(item => (
                        item.GetProperty("id").GetString() ?? "",
                        item.GetProperty("name").GetString() ?? ""
                    ))
                    .ToList();

                foreach (var (id, name) in playlists)
                {
                    if (name == "Anthem Queue")
                        return ServiceResult<string?>.Success(id);
                }

                offset += limit;
            }
            while (playlists.Count == limit);

            // Create the 'Anthem Queue' playlist
            var data = new Dictionary<string, string>
            {
                { "description", "These were the tracks from you most recent feed or profile visit on Anthem." },
                { "name", "Anthem Queue" },
                { "public", "false" }
            };
            response = await _client.PostAsync($"users/{userId}/playlists", JsonContent.Create(data));
            content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return ServiceResult<string?>.Failure(null, $"Error response pt.2: {response}", "SpotifyService.GetFeedPlaylistUri()");

            json = JsonDocument.Parse(content).RootElement;

            string? playlistId = json.GetProperty("id").GetString();

            if (playlistId == null)
                return ServiceResult<string?>.Failure(null, "Playlist Id was null.", "SpotifyService.GetFeedPlaylistUri()");
            
            return ServiceResult<string?>.Success(playlistId);
        }
        catch (Exception e)
        {
            return ServiceResult<string?>.Failure(e, "Failed to get feed playlist Id.", "SpotifyService.GetFeedPlaylistId()");
        }
    }

    public async Task<string?> GetPreviewUrl(string trackUri)
    {
        try
        {
            HttpResponseMessage response = await _previewUrlClient.GetAsync(trackUri.Split(":").Last());

            if (!response.IsSuccessStatusCode)
                return null;

            string htmlContent = await response.Content.ReadAsStringAsync();

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            var metaTag = htmlDoc.DocumentNode.SelectSingleNode("//meta[@property='og:audio']");

            return metaTag?.GetAttributeValue("content", string.Empty);
        }
        catch
        {
            return null;
        }
    }

    public async Task<ServiceResult<Status?>> GetStatus(string accessToken, string userId)
    {
        try
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await _client.GetAsync("me/player");

            if (!response.IsSuccessStatusCode)
                return ServiceResult<Status?>.Failure(null, $"Error response: {response}", "SpotifyService.GetStatus()");

            string content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(content))
                return ServiceResult<Status?>.Success(null);

            JsonElement json = JsonDocument.Parse(content).RootElement;

            // Ensure they are listening to a Track
            string type = json.GetProperty("currently_playing_type").GetString()!;

            if (type != "track")
                return ServiceResult<Status?>.Success(null);

            JsonElement item = json.GetProperty("item");

            Track track = GetTrack(item);

            // Add Preview URL
            track.PreviewUrl = await GetPreviewUrl(track.Uri);

            // Create Status
            var status = new Status
            {
                UserId = userId,
                Track = track,
                LastChanged = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            return ServiceResult<Status?>.Success(status);
        }
        catch (Exception e)
        {
            return ServiceResult<Status?>.Failure(e, $"Failed to get status for {userId}.", "SpotifyService.GetStatus()");
        }
    }

    public async Task<ServiceResult<bool>> SaveAlbum(string accessToken, string id)
    {
        try
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var data = new
            {
                ids = new List<string>() { id }
            };

            HttpResponseMessage response = await _client.PutAsync("me/albums", JsonContent.Create(data));

            if (!response.IsSuccessStatusCode)
                return ServiceResult<bool>.Failure(null, $"Error response: {response}", "SpotifyService.SaveAlbum()");

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception e)
        {
            return ServiceResult<bool>.Failure(e, "Failed to save album.", "SpotifyService.SaveAlbum()");
        }
    }

    public async Task<ServiceResult<bool>> UnsaveAlbum(string accessToken, string id)
    {
        try
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await _client.DeleteAsync($"me/albums?ids={id}");

            if (!response.IsSuccessStatusCode)
                return ServiceResult<bool>.Failure(null, $"Error response: {response}", "SpotifyService.UnsaveAlbum()");

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception e)
        {
            return ServiceResult<bool>.Failure(e, "Failed to unsave album.", "SpotifyService.UnsaveAlbum()");
        }
    }

    public async Task<ServiceResult<bool>> SaveTrack(string accessToken, string id)
    {
        try
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var data = new
            {
                ids = new List<string>() { id }
            };

            HttpResponseMessage response = await _client.PutAsync("me/tracks", JsonContent.Create(data));

            if (!response.IsSuccessStatusCode)
                return ServiceResult<bool>.Failure(null, $"Error response: {response}", "SpotifyService.SaveTrack()");

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception e)
        {
            return ServiceResult<bool>.Failure(e, "Failed to save track.", "SpotifyService.SaveTrack()");
        }
    }

    public async Task<ServiceResult<bool>> UnsaveTrack(string accessToken, string id)
    {
        try
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await _client.DeleteAsync($"me/tracks?ids={id}");

            if (!response.IsSuccessStatusCode)
                return ServiceResult<bool>.Failure(null, $"Error response: {response}", "SpotifyService.UnsaveTrack()");

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception e)
        {
            return ServiceResult<bool>.Failure(e, "Failed to unsave track.", "SpotifyService.UnsaveTrack()");
        }
    }
    
    public async Task<ServiceResult<bool>> UpdateAnthemQueuePlaylist(string accessToken, string playlistId, List<string> trackUris)
    {
        try
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var data = new Dictionary<string, object>
            {
                { "uris", trackUris },
            };
            HttpResponseMessage response = await _client.PutAsync($"playlists/{playlistId}/tracks", JsonContent.Create(data));

            if (!response.IsSuccessStatusCode)
                return ServiceResult<bool>.Failure(null, $"Error response: {response}", "SpotifyService.UpdateFeedPlaylist()");

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception e)
        {
            return ServiceResult<bool>.Failure(e, "Failed to update feed playlist.", "SpotifyService.UpdateFeedPlaylist()");
        }
    }

    public async Task<ServiceResult<List<Album>>> SearchAlbums(string accessToken, string query)
    {
        try
        {
            string type = "album";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await _client.GetAsync($"search?type={type}&limit={SPOTIFY_SEARCH_ITEMS_LIMIT}&q={query}");

            if (!response.IsSuccessStatusCode)
                return ServiceResult<List<Album>>.Failure(null, $"Error response: {response}", "SpotifyService.SearchAlbums()");

            string content = await response.Content.ReadAsStringAsync();
            JsonElement json = JsonDocument.Parse(content).RootElement;
            List<Album> albums = json
                .GetProperty("albums")
                .GetProperty("items")
                .EnumerateArray()
                .Select(GetAlbum)
                .ToList();

            return ServiceResult<List<Album>>.Success(albums);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Album>>.Failure(e, $"Query: {query}.", "SpotifyService.SearchAlbums()");
        }
    }

    public async Task<ServiceResult<List<Artist>>> SearchArtists(string accessToken, string query)
    {
        try
        {
            string type = "artist";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await _client.GetAsync($"search?type={type}&limit={SPOTIFY_SEARCH_ITEMS_LIMIT}&q={query}");

            if (!response.IsSuccessStatusCode)
                return ServiceResult<List<Artist>>.Failure(null, $"Error response: {response}", "SpotifyService.SearchArtists()");

            string content = await response.Content.ReadAsStringAsync(); 
            JsonElement json = JsonDocument.Parse(content).RootElement;
            List<Artist> artists = json
                .GetProperty("artists")
                .GetProperty("items")
                .EnumerateArray()
                .Select(GetArtist)
                .ToList();

            return ServiceResult<List<Artist>>.Success(artists);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Artist>>.Failure(e, $"Query: {query}.", "SpotifyService.SearchArtists()");
        }
    }

    public async Task<ServiceResult<List<Track>>> SearchTracks(string accessToken, string query)
    {
        try
        {
            string type = "track";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await _client.GetAsync($"search?type={type}&limit={SPOTIFY_SEARCH_ITEMS_LIMIT}&q={query}");

            if (!response.IsSuccessStatusCode)
                return ServiceResult<List<Track>>.Failure(null, $"Error response: {response}", "SpotifyService.SearchTracks()");

            string content = await response.Content.ReadAsStringAsync(); 
            JsonElement json = JsonDocument.Parse(content).RootElement;
            List<Track> tracks = (await Task.WhenAll(
                json
                    .GetProperty("tracks")
                    .GetProperty("items")
                    .EnumerateArray()
                    .Select(GetTrack)
                    .Select(async track =>
                    {
                        // Add Preview URL
                        track.PreviewUrl = await GetPreviewUrl(track.Uri);
                        return track;
                    })
            )).ToList();

            return ServiceResult<List<Track>>.Success(tracks);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Track>>.Failure(e, $"Query: {query}.", "SpotifyService.SearchTracks()");
        }
    }
}
