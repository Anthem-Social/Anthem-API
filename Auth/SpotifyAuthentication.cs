using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using AnthemAPI.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using static AnthemAPI.Common.Constants;

namespace AnthemAPI.Authentication;

public class SpotifyAuthenticationOptions : AuthenticationSchemeOptions
{
    public string AuthenticationScheme => Spotify;
}

public class SpotifyAuthenticationHandler : AuthenticationHandler<SpotifyAuthenticationOptions>
{
    private readonly HttpClient _client;

    public SpotifyAuthenticationHandler
    (
        IOptionsMonitor<SpotifyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IHttpClientFactory factory
    ) : base(options, logger, encoder)
    {
        _client = factory.CreateClient();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            if (!Request.Headers.ContainsKey("Authorization"))
                return AuthenticateResult.Fail("Authorization header not found.");

            string authorization = Request.Headers["Authorization"].ToString();

            if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return AuthenticateResult.Fail("Bearer token not found.");

            string token = authorization.Substring("Bearer ".Length).Trim();
        
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await _client.GetAsync("https://api.spotify.com/v1/me");

            if (!response.IsSuccessStatusCode)
                return AuthenticateResult.Fail("Invalid token or API call failed.");

            string content = await response.Content.ReadAsStringAsync();
            JsonElement json = JsonDocument.Parse(content).RootElement;

            // Create Spotify Authentication claims
            string id = json.GetProperty("id").GetString()!;
            string country = json.GetProperty("country").GetString()!;
            bool explicitContent = json.GetProperty("explicit_content").GetProperty("filter_enabled").GetBoolean()!;
            bool premium = json.GetProperty("product").GetString()! == "premium";

            var claims = new[]
            {
                new Claim("access_token", token),
                new Claim("country", country),
                new Claim("explicit_content", explicitContent.ToString()),
                new Claim("premium", premium.ToString()),
                new Claim("user_id", id)
            };

            var identity = new ClaimsIdentity(claims, Spotify);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Spotify);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception e)
        {
            _ = ServiceResult<bool>.Failure(e, "Authentication failed.", "SpotifyAuthenticationHandler.HandleAuthenticationAsync()");
            return AuthenticateResult.Fail($"Authentication failed: {e}");
        }
    }
}
