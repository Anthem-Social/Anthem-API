using System.Net.Http.Headers;
using AnthemAPI.Common;

namespace AnthemAPI.Services;

public class SpotifyService
{
    private readonly HttpClient _spotifyHttpClient;

    public SpotifyService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        var authHeader = httpContextAccessor.HttpContext.Request.Headers.Authorization.ToString();
        string accessToken;

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            accessToken = authHeader.Split(" ")[1].Trim();
            Console.WriteLine($"Extracted Token: {accessToken}");
            _spotifyHttpClient = httpClient;
            _spotifyHttpClient.BaseAddress = new Uri("https://api.spotify.com/v1/");
            _spotifyHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public async Task<ServiceResult<string>> GetMe()
    {
        var response = await _spotifyHttpClient.GetAsync("me");
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return ServiceResult<string>.Success(content);
        }
        else
        {
            return ServiceResult<string>.Failure($"Error response from fetching me: {response}", "SpotifyService.GetMe()");
        }
    }

    public async Task<ServiceResult<string>> GetJake()
    {
        var response = await _spotifyHttpClient.GetAsync("users/jacob.day");
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return ServiceResult<string>.Success(content);
        }
        else
        {
            return ServiceResult<string>.Failure($"Error response from fetching Jake: {response}", "SpotifyService.GetJake()");
        }
    }


}