using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.AspNetCore.Mvc;

namespace AnthemAPI.Controllers;

[ApiController]
[Route("spotify")]
public class SpotifyController : ControllerBase
{
    private readonly string _authHeader;
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _configuration;
    private readonly string _encryptionMethod;
    private readonly byte[] _encryptionSecret;

    public SpotifyController(IHttpClientFactory clientFactory, IConfiguration configuration)
    {
        _clientFactory = clientFactory;
        _configuration = configuration;

        var clientId = configuration["SpotifyClientId"];
        var clientSecret = configuration["SpotifyClientSecret"];
        _authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        using (var sha256 = SHA256.Create())
        {
            _encryptionSecret = sha256.ComputeHash(Encoding.UTF8.GetBytes(_configuration["EncryptionSecret"]));
        }
        _encryptionMethod = _configuration["EncryptionMethod"] ?? "aes-256-ctr";
    }

    private string Encrypt(string text)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = _encryptionSecret;
            aes.GenerateIV();
            var iv = aes.IV;

            using (var encryptor = aes.CreateEncryptor(aes.Key, iv))
            using (var ms = new MemoryStream())
            {
                ms.Write(iv, 0, iv.Length);
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(text);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    private string Decrypt(string text)
    {
        var fullCipher = Convert.FromBase64String(text);
        using (var aes = Aes.Create())
        {
            aes.Key = _encryptionSecret;
            var iv = new byte[aes.BlockSize / 8];
            var cipher = new byte[fullCipher.Length - iv.Length];

            Array.Copy(fullCipher, iv, iv.Length);
            Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            using (var decryptor = aes.CreateDecryptor(aes.Key, iv))
            using (var ms = new MemoryStream(cipher))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }

    private async Task<HttpResponseMessage> PostRequestAsync(string url, Dictionary<string, string> data)
    {
        var client = _clientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(data)
        };
        request.Headers.Add("Authorization", $"Basic {_authHeader}");
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

        return await client.SendAsync(request);
    }

    [HttpPost("token/swap")]
    public async Task<IActionResult> TokenSwap([FromForm] string code)
    {
        Console.WriteLine("SWAP: " + code);
        var data = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "redirect_uri", _configuration["SpotifyCallback"] },
            { "code", code }
        };

        var response = await PostRequestAsync("https://accounts.spotify.com/api/token", data);
        var result = await response.Content.ReadAsStringAsync();

        Console.WriteLine(result);

        if (response.IsSuccessStatusCode)
        {
            var json = System.Text.Json.JsonDocument.Parse(result);
            if (json.RootElement.TryGetProperty("refresh_token", out var refreshToken))
            {
                var encryptedToken = Encrypt(refreshToken.GetString());
                var jsonObject = json.RootElement.GetRawText().Replace(refreshToken.GetString(), encryptedToken);
                return Ok(jsonObject);
            }
            return Ok(result);
        }
        return StatusCode((int)response.StatusCode, result);
    }

    [HttpPost("token/refresh")]
    public async Task<IActionResult> TokenRefresh([FromForm] string refresh_token)
    {
        Console.WriteLine("REFRESH: " + refresh_token);
        var decryptedToken = Decrypt(refresh_token);
        var data = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", decryptedToken }
        };

        var response = await PostRequestAsync("https://accounts.spotify.com/api/token", data);
        var result = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var json = System.Text.Json.JsonDocument.Parse(result);
            if (json.RootElement.TryGetProperty("refresh_token", out var newRefreshToken))
            {
                var encryptedToken = Encrypt(newRefreshToken.GetString());
                var jsonObject = json.RootElement.GetRawText().Replace(newRefreshToken.GetString(), encryptedToken);
                return Ok(jsonObject);
            }
            return Ok(result);
        }
        return StatusCode((int)response.StatusCode, result);

    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe([FromHeader(Name = "Authorization")] string authorization)
    {
        var client = _clientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", authorization);

        var response = await client.GetAsync("https://api.spotify.com/v1/me");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return Ok(content);
        }
        else
        {
            return StatusCode((int)response.StatusCode, "Failed to retrieve Spotify profile");
        }
    }
}
