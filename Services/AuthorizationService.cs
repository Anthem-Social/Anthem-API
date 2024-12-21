using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AnthemAPI.Common;
using AnthemAPI.Models;

namespace AnthemAPI.Services;

public class AuthorizationService
{
    private readonly byte[] _encryptionKey;
    private readonly DynamoDBContext _context;
    private readonly HttpClient _client;
    private readonly IConfiguration _configuration;
    private readonly SpotifyService _spotifyService;

    public AuthorizationService(
        IAmazonDynamoDB dbClient,
        HttpClient client,
        IConfiguration configuration,
        SpotifyService spotifyService
    )
    {
        _configuration = configuration;
        _spotifyService = spotifyService;
        _context = new DynamoDBContext(dbClient);

        using (var sha256 = SHA256.Create())
        {
            _encryptionKey = sha256.ComputeHash(Encoding.UTF8.GetBytes(_configuration["EncryptionKey"]!));
        }

        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{configuration["SpotifyClientId"]}:{configuration["SpotifyClientSecret"]}"));
        _client = client;
        _client.BaseAddress = new Uri("https://accounts.spotify.com/api/token");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
    }

    private string Encrypt(string text)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = _encryptionKey;
            aes.GenerateIV();

            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream())
            {
                ms.Write(aes.IV, 0, aes.IV.Length);
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
        byte[] fullCipher = Convert.FromBase64String(text);

        using (var aes = Aes.Create())
        {
            aes.Key = _encryptionKey;
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
                return ServiceResult<string>.Failure($"Error response for token swap: {response}", "AuthorizationService.Swap()");
            }

            JsonElement json = JsonDocument.Parse(content).RootElement;

            if (json.TryGetProperty("refresh_token", out var refreshToken))
            {
                string encrypted = Encrypt(refreshToken.GetString()!);
                string result = json.GetRawText().Replace(refreshToken.GetString()!, encrypted);

                return ServiceResult<string>.Success(result);
            }

            return ServiceResult<string>.Failure($"No refresh_token property in JSON. {json}", "AuthorizationService.Swap()");
        }
        catch (Exception e)
        {
            return ServiceResult<string>.Failure($"Failed to swap.\nError: {e}", "AuthorizationService.Swap()");
        }
    }

    public async Task<ServiceResult<string>> Refresh(string refreshToken)
    {
        try
        {
            string decrypted = Decrypt(refreshToken);
            var data = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", decrypted }
            };
            HttpResponseMessage response = await _client.PostAsync("", new FormUrlEncodedContent(data));
            string content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return ServiceResult<string>.Failure($"Error response for token refresh: {response}", "AuthorizationService.Refresh()");
            }

            JsonElement json = JsonDocument.Parse(content).RootElement;

            if (json.TryGetProperty("refresh_token", out var token))
            {
                string encrypted = Encrypt(token.GetString()!);
                string result = json.GetRawText().Replace(token.GetString()!, encrypted);

                return ServiceResult<string>.Success(result);
            }
            
            return ServiceResult<string>.Failure($"No refresh_token property in JSON. {json}", "AuthorizationService.Refresh()");
        }
        catch (Exception e)
        {
            return ServiceResult<string>.Failure($"Failed to refresh token.\n{e}", "AuthorizationService.Refresh()");
        }
    }

        public async Task<ServiceResult<Authorization?>> Load(string userId)
    {
        try
        {
            var authorization = await _context.LoadAsync<Authorization>(userId);
            return ServiceResult<Authorization?>.Success(authorization);
        }
        catch (Exception e)
        {
            return ServiceResult<Authorization?>.Failure($"Failed to load authorization for user {userId}.\n{e}", "AuthorizationService.Load()");
        }
    }

    public async Task<ServiceResult<Authorization>> Save(Authorization authorization)
    {
        try
        {
            await _context.SaveAsync(authorization);
            return ServiceResult<Authorization>.Success(authorization);
        }
        catch (Exception e)
        {
            return ServiceResult<Authorization>.Failure($"Failed to save authorization for user {authorization.UserId}.\n{e}", "AuthorizationService.Save()");
        }
    }

    public async Task<ServiceResult<Authorization>> Save(JsonElement json)
    {
        try
        {
            string accessToken = json.GetProperty("access_token").GetString()!;
            string refreshToken = json.GetProperty("refresh_token").GetString()!;
            long expiresAt = json.GetProperty("expires_in").GetInt64() + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            var result = await _spotifyService.GetId(accessToken);

            if (result.Data is null || result.IsFailure)
            {
                return ServiceResult<Authorization>.Failure($"Failed to get id.\n{result.ErrorMessage}", "AuthorizationService.Save()");
            }

            string id = result.Data!;
            var authorization = new Authorization
            {
                UserId = id,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt
            };

            return await Save(authorization);
        }
        catch (Exception e)
        {
            return ServiceResult<Authorization>.Failure($"Failed to save authorization by json.\n{e}", "AuthorizationService.Save()");
        }
    }
}
