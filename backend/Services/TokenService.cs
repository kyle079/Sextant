using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Sextant.Data;

namespace Sextant.Services;

public record SsoTokens(string AccessToken, string RefreshToken, int ExpiresIn);

public class TokenService(
    IDataProtectionProvider dataProtectionProvider,
    AppDbContext db,
    IMemoryCache cache,
    IHttpClientFactory httpClientFactory,
    IConfiguration config)
{
    private readonly IDataProtector _protector =
        dataProtectionProvider.CreateProtector("Sextant.RefreshTokens");

    public string Encrypt(string refreshToken) => _protector.Protect(refreshToken);
    public string Decrypt(string encrypted) => _protector.Unprotect(encrypted);

    public async Task<string> GetAccessToken(long characterId)
    {
        var cacheKey = $"access:{characterId}";
        if (cache.TryGetValue(cacheKey, out string? cached))
            return cached!;

        var token = await db.CharacterTokens
            .FirstOrDefaultAsync(t => t.CharacterId == characterId && t.IsActive)
            ?? throw new InvalidOperationException($"No active token for character {characterId}");

        var refreshToken = Decrypt(token.EncryptedRefreshToken);
        var tokens = await ExchangeRefreshToken(refreshToken);

        token.EncryptedRefreshToken = Encrypt(tokens.RefreshToken);
        token.LastRefreshed = DateTime.UtcNow;
        await db.SaveChangesAsync();

        cache.Set(cacheKey, tokens.AccessToken,
            TimeSpan.FromSeconds(Math.Max(tokens.ExpiresIn - 60, 60)));

        return tokens.AccessToken;
    }

    public async Task<SsoTokens> ExchangeCodeForTokens(string code)
    {
        var response = await PostToTokenEndpoint(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
        });
        return response;
    }

    public async Task<string> RefreshAccessToken(long characterId)
    {
        cache.Remove($"access:{characterId}");
        return await GetAccessToken(characterId);
    }

    private async Task<SsoTokens> ExchangeRefreshToken(string refreshToken)
    {
        return await PostToTokenEndpoint(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        });
    }

    private async Task<SsoTokens> PostToTokenEndpoint(Dictionary<string, string> form)
    {
        var client = httpClientFactory.CreateClient();
        var clientId = config["ESI:ClientId"] ?? throw new InvalidOperationException("ESI:ClientId not configured");
        var clientSecret = config["ESI:ClientSecret"] ?? throw new InvalidOperationException("ESI:ClientSecret not configured");
        var tokenUrl = config["ESI:TokenUrl"]!;

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Basic", credentials) },
            Content = new FormUrlEncodedContent(form),
        };

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = doc.RootElement;

        return new SsoTokens(
            root.GetProperty("access_token").GetString()!,
            root.GetProperty("refresh_token").GetString()!,
            root.GetProperty("expires_in").GetInt32());
    }

    public static (long CharacterId, string CharacterName) ParseJwt(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
            throw new InvalidOperationException("Invalid JWT");

        var padded = parts[1].Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');

        using var doc = JsonDocument.Parse(Convert.FromBase64String(padded));
        var root = doc.RootElement;

        var sub = root.GetProperty("sub").GetString()!;
        var name = root.GetProperty("name").GetString()!;
        var characterId = long.Parse(sub.Split(':')[^1]);

        return (characterId, name);
    }
}
