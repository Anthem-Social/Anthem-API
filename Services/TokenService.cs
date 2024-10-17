using AnthemAPI.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace AnthemAPI.Services;

public class TokenService
{
    private readonly IConfiguration _configuration;
    private readonly byte[] _encryptionSecret;
    private readonly HttpClient _tokenHttpClient;
    public TokenService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;

        var basicAuth = Convert.ToBase64String(Encoding.UTF8. GetBytes($"{configuration["SpotifyClientId"]}:{configuration["SpotifyClientSecret"]}"));
        Console.WriteLine("Basic Auth: " + basicAuth);

        _tokenHttpClient = httpClient;
        _tokenHttpClient.BaseAddress = new Uri("https://accounts.spotify.com/api/token");
        _tokenHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

        using (var sha256 = SHA256.Create())
        {
            _encryptionSecret = sha256.ComputeHash(Encoding.UTF8.GetBytes(_configuration["EncryptionSecret"]!));
        }
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

    public async Task<ServiceResult<string>> Swap(string code)
    {
        var data = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "redirect_uri", _configuration["SpotifyCallback"]! },
            { "code", code }
        };

        var response = await _tokenHttpClient.PostAsync("", new FormUrlEncodedContent(data));
        var result = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var json = System.Text.Json.JsonDocument.Parse(result);
            if (json.RootElement.TryGetProperty("refresh_token", out var refreshToken))
            {
                var encryptedToken = Encrypt(refreshToken.GetString()!);
                var jsonObject = json.RootElement.GetRawText().Replace(refreshToken.GetString()!, encryptedToken);
                return ServiceResult<string>.Success(jsonObject);
            }

            return ServiceResult<string>.Failure($"No refresh_token property in JSON. {json}", "TokenService.Swap()");
        }

        return ServiceResult<string>.Failure($"Error response for token swap: {response}", "TokenService.Swap()");
    }

    public async Task<ServiceResult<string>> Refresh(string refresh_token)
    {
        var decryptedToken = Decrypt(refresh_token);
        var data = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", decryptedToken }
        };

        var response = await _tokenHttpClient.PostAsync("", new FormUrlEncodedContent(data));
        var result = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var json = System.Text.Json.JsonDocument.Parse(result);
            if (json.RootElement.TryGetProperty("refresh_token", out var newRefreshToken))
            {
                var encryptedToken = Encrypt(newRefreshToken.GetString()!);
                var jsonObject = json.RootElement.GetRawText().Replace(newRefreshToken.GetString()!, encryptedToken);
                return ServiceResult<string>.Success(jsonObject);
            }
            return ServiceResult<string>.Failure($"No refresh_token property in JSON. {json}", "TokenService.Refresh()");
        }

        return ServiceResult<string>.Failure($"Error response for token refresh: {response}", "TokenService.Refresh()");
    }
}