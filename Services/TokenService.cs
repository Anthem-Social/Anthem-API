using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AnthemAPI.Common;

namespace AnthemAPI.Services;

public class TokenService
{
    private readonly HttpClient _client;
    private readonly IConfiguration _configuration;

    public TokenService(IHttpClientFactory factory, IConfiguration configuration)
    {
        _configuration = configuration;
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_configuration["SpotifyClientId"]}:{_configuration["SpotifyClientSecret"]}"));
        _client = factory.CreateClient();
        _client.BaseAddress = new Uri("https://accounts.spotify.com/api/token");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
    }

    public async Task<ServiceResult<string>> Swap(string code)
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "redirect_uri", _configuration["SpotifyCallback"]! },
                { "code", code }
            };
            HttpResponseMessage response = await _client.PostAsync("", new FormUrlEncodedContent(data));
            string content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return ServiceResult<string>.Failure(null, $"Error response: {response}", "TokenService.Swap()");
            }

            JsonElement json = JsonDocument.Parse(content).RootElement;

            if (json.TryGetProperty("refresh_token", out var refreshToken))
            {
                string encrypted = Utility.Encrypt(_configuration["EncryptionKey"]!, refreshToken.GetString()!);
                string result = json.GetRawText().Replace(refreshToken.GetString()!, encrypted);

                return ServiceResult<string>.Success(result);
            }

            return ServiceResult<string>.Failure(null, $"No refresh_token property. {json}", "TokenService.Swap()");
        }
        catch (Exception e)
        {
            return ServiceResult<string>.Failure(e, "Failed to swap.", "TokenService.Swap()");
        }
    }

    public async Task<ServiceResult<string>> Refresh(string refreshToken)
    {
        try
        {
            string decrypted = Utility.Decrypt(_configuration["EncryptionKey"]!, refreshToken);
            var data = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", decrypted }
            };
            HttpResponseMessage response = await _client.PostAsync("", new FormUrlEncodedContent(data));
            string content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return ServiceResult<string>.Failure(null, $"Error response: {response}", "TokenService.Refresh()");
            }

            JsonElement json = JsonDocument.Parse(content).RootElement;

            return ServiceResult<string>.Success(json.GetRawText());
        }
        catch (Exception e)
        {
            return ServiceResult<string>.Failure(e, "Failed to refresh.", "TokenService.Refresh()");
        }
    }
}
