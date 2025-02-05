using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;

namespace AnthemAPI.Common;

public static class Utility
{
    private static byte[] ToBytes(string key)
    {
        using (var sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        }
    }

    public static string Encrypt(string key, string text)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = ToBytes(key);
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

    public static string Decrypt(string key, string text)
    {
        byte[] fullCipher = Convert.FromBase64String(text);

        using (var aes = Aes.Create())
        {
            aes.Key = ToBytes(key);
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

    public static string AddRefreshTokenProperty(string json, string refreshToken)
    {
        var document = JsonDocument.Parse(json);
        var dictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(document.RootElement)!;
        dictionary["refresh_token"] = refreshToken;
        return JsonSerializer.Serialize(dictionary);
    }

    public static DateTime ToDateTimeUTC(string iso8601)
    {
        return DateTime.Parse(iso8601, null, DateTimeStyles.AssumeUniversal);
    }

    public static async Task<List<string>> SendToConnections(string url, HashSet<string> connectionIds, object message)
    {
        var config = new AmazonApiGatewayManagementApiConfig
        {
            ServiceURL = url
        };
        using var client = new AmazonApiGatewayManagementApiClient(config);
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        string json = JsonSerializer.Serialize(message, options);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        var gone = new ConcurrentBag<string>();

        var posts = connectionIds.Select(async connectionId =>
        {
            try
            {
                await client.PostToConnectionAsync(new PostToConnectionRequest
                {
                    ConnectionId = connectionId,
                    Data = new MemoryStream(bytes)
                });
                Console.WriteLine($"Sending to {connectionId}.");
            }
            catch (GoneException)
            {
                gone.Add(connectionId);
                Console.WriteLine("Adding " + connectionId + " to gone.");
            }
        });

        await Task.WhenAll(posts);

        return gone.ToList();
    }
}
