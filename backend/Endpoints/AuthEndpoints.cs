using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Sextant.Data;
using Sextant.Models;
using Sextant.Services;

namespace Sextant.Endpoints;

public static class AuthEndpoints
{
    private const string StateCookie = "sextant_oauth_state";

    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapGet("/login", (Delegate)Login).AllowAnonymous();
        group.MapGet("/callback", (Delegate)Callback).AllowAnonymous();
        group.MapPost("/logout", (Delegate)Logout).RequireAuthorization();
        group.MapGet("/me", (Delegate)Me).RequireAuthorization();
        group.MapPost("/characters", (Delegate)AddCharacter).RequireAuthorization();
        group.MapDelete("/characters/{characterId:long}", (Delegate)RemoveCharacter).RequireAuthorization();
    }

    // GET /auth/login — redirect to Eve SSO
    static IResult Login(HttpContext ctx, IConfiguration config)
    {
        var state = GenerateState();
        var statePayload = JsonSerializer.Serialize(new { nonce = state, action = "login" });
        SetStateCookie(ctx, statePayload);

        var url = BuildSsoUrl(config, state);
        return Results.Redirect(url);
    }

    // GET /auth/callback — exchange code, validate corp, create/update user
    static async Task<IResult> Callback(
        HttpContext ctx,
        AppDbContext db,
        TokenService tokenService,
        IHttpClientFactory httpFactory,
        IConfiguration config)
    {
        var code = ctx.Request.Query["code"].ToString();
        var returnedState = ctx.Request.Query["state"].ToString();

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(returnedState))
            return Results.Redirect("/login?error=missing_params");

        // Verify state
        var stateCookieValue = ctx.Request.Cookies[StateCookie];
        if (string.IsNullOrEmpty(stateCookieValue))
            return Results.Redirect("/login?error=no_state");

        OAuthState? oauthState;
        try
        {
            oauthState = JsonSerializer.Deserialize<OAuthState>(stateCookieValue,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch { return Results.Redirect("/login?error=invalid_state"); }

        if (oauthState is null || oauthState.Nonce != returnedState)
            return Results.Redirect("/login?error=state_mismatch");

        ClearStateCookie(ctx);

        // Exchange code for tokens
        SsoTokens tokens;
        try { tokens = await tokenService.ExchangeCodeForTokens(code); }
        catch { return Results.Redirect("/login?error=token_exchange_failed"); }

        // Parse JWT to get character info
        var (characterId, characterName) = TokenService.ParseJwt(tokens.AccessToken);

        // Get corporation ID from ESI
        int corporationId;
        try { corporationId = await GetCorporationId(httpFactory, characterId); }
        catch { return Results.Redirect("/login?error=esi_error"); }

        // Corp gate (0 = open)
        var allowedCorpId = config.GetValue<int>("App:AllowedCorpId");
        if (allowedCorpId != 0 && corporationId != allowedCorpId)
            return Results.Redirect("/login?error=wrong_corp");

        var scopes = config["ESI:Scopes"] ?? string.Empty;
        var encryptedRefresh = tokenService.Encrypt(tokens.RefreshToken);

        if (oauthState.Action == "link" && oauthState.UserId.HasValue)
        {
            // Linking an alt to an existing account
            await UpsertCharacterToken(db, oauthState.UserId.Value, characterId, characterName,
                corporationId, encryptedRefresh, scopes);
            await db.SaveChangesAsync();
            return Results.Redirect("/settings?tab=characters");
        }

        // Login flow: find or create user
        var existingToken = await db.CharacterTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.CharacterId == characterId);

        User user;
        if (existingToken is not null)
        {
            user = existingToken.User;
            user.LastLogin = DateTime.UtcNow;
            user.CharacterName = characterName;
            existingToken.EncryptedRefreshToken = encryptedRefresh;
            existingToken.CorporationId = corporationId;
            existingToken.LastRefreshed = DateTime.UtcNow;
            existingToken.IsActive = true;
        }
        else
        {
            var isFirstUser = !await db.Users.AnyAsync();
            user = new User
            {
                CharacterName = characterName,
                PrimaryCharacterId = characterId,
                Role = isFirstUser ? UserRole.Admin : UserRole.Member,
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.CharacterTokens.Add(new CharacterToken
            {
                UserId = user.Id,
                CharacterId = characterId,
                CharacterName = characterName,
                CorporationId = corporationId,
                EncryptedRefreshToken = encryptedRefresh,
                Scopes = scopes,
                LastRefreshed = DateTime.UtcNow,
                IsActive = true,
            });
        }

        await db.SaveChangesAsync();
        await SignIn(ctx, user, characterId);

        return Results.Redirect("/");
    }

    // POST /auth/logout
    static async Task<IResult> Logout(HttpContext ctx)
    {
        await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Ok();
    }

    // GET /auth/me
    static async Task<IResult> Me(HttpContext ctx, AppDbContext db)
    {
        var userId = GetUserId(ctx);
        var user = await db.Users
            .Include(u => u.Characters.Where(c => c.IsActive))
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return Results.Unauthorized();

        return Results.Ok(new
        {
            id = user.Id,
            characterName = user.CharacterName,
            primaryCharacterId = user.PrimaryCharacterId,
            role = user.Role.ToString(),
            characters = user.Characters.Select(c => new
            {
                characterId = c.CharacterId,
                characterName = c.CharacterName,
                corporationId = c.CorporationId,
                isActive = c.IsActive,
                portraitUrl = $"https://images.evetech.net/characters/{c.CharacterId}/portrait?size=64",
            }),
        });
    }

    // POST /auth/characters — start SSO to link an alt
    static IResult AddCharacter(HttpContext ctx, IConfiguration config)
    {
        var userId = GetUserId(ctx);
        var state = GenerateState();
        var statePayload = JsonSerializer.Serialize(new { nonce = state, action = "link", userId });
        SetStateCookie(ctx, statePayload);

        return Results.Ok(new { url = BuildSsoUrl(config, state) });
    }

    // DELETE /auth/characters/{characterId}
    static async Task<IResult> RemoveCharacter(long characterId, HttpContext ctx, AppDbContext db)
    {
        var userId = GetUserId(ctx);
        var token = await db.CharacterTokens
            .FirstOrDefaultAsync(t => t.CharacterId == characterId && t.UserId == userId);

        if (token is null) return Results.NotFound();

        // Don't allow removing the primary character (only alt removal supported here)
        var user = await db.Users.FindAsync(userId);
        if (user?.PrimaryCharacterId == characterId) return Results.BadRequest("Cannot remove primary character.");

        token.IsActive = false;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    // --- Helpers ---

    static string GenerateState() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    static string BuildSsoUrl(IConfiguration config, string state)
    {
        var clientId = config["ESI:ClientId"];
        var callbackUrl = config["ESI:CallbackUrl"];
        var scopes = config["ESI:Scopes"];
        var authUrl = config["ESI:AuthUrl"];

        return $"{authUrl}?response_type=code&client_id={Uri.EscapeDataString(clientId ?? "")}" +
               $"&redirect_uri={Uri.EscapeDataString(callbackUrl ?? "")}" +
               $"&scope={Uri.EscapeDataString(scopes ?? "")}" +
               $"&state={Uri.EscapeDataString(state)}";
    }

    static void SetStateCookie(HttpContext ctx, string value) =>
        ctx.Response.Cookies.Append(StateCookie, value, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10),
            IsEssential = true,
        });

    static void ClearStateCookie(HttpContext ctx) =>
        ctx.Response.Cookies.Delete(StateCookie);

    static async Task UpsertCharacterToken(AppDbContext db, int userId, long characterId,
        string characterName, int corporationId, string encryptedRefresh, string scopes)
    {
        var existing = await db.CharacterTokens
            .FirstOrDefaultAsync(t => t.CharacterId == characterId);

        if (existing is not null)
        {
            existing.UserId = userId;
            existing.CharacterName = characterName;
            existing.CorporationId = corporationId;
            existing.EncryptedRefreshToken = encryptedRefresh;
            existing.LastRefreshed = DateTime.UtcNow;
            existing.IsActive = true;
        }
        else
        {
            db.CharacterTokens.Add(new CharacterToken
            {
                UserId = userId,
                CharacterId = characterId,
                CharacterName = characterName,
                CorporationId = corporationId,
                EncryptedRefreshToken = encryptedRefresh,
                Scopes = scopes,
                LastRefreshed = DateTime.UtcNow,
                IsActive = true,
            });
        }
    }

    static async Task SignIn(HttpContext ctx, User user, long activeCharacterId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.CharacterName),
            new("role", user.Role.ToString()),
            new("activeCharacterId", activeCharacterId.ToString()),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });
    }

    static async Task<int> GetCorporationId(IHttpClientFactory httpFactory, long characterId)
    {
        var client = httpFactory.CreateClient();
        using var doc = await client.GetFromJsonAsync<JsonDocument>(
            $"https://esi.evetech.net/latest/characters/{characterId}/");
        return doc!.RootElement.GetProperty("corporation_id").GetInt32();
    }

    static int GetUserId(HttpContext ctx) =>
        int.Parse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    record OAuthState(string Nonce, string Action, int? UserId = null);
}
