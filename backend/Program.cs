using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Sextant.Data;
using Sextant.Endpoints;
using Sextant.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
var dbPath = builder.Configuration["Database:Path"] ?? "data/sextant.db";
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(dbPath))!);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Data Protection — persists encryption keys across restarts
var keysPath = builder.Environment.IsDevelopment() ? "keys" : "/app/keys";
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("Sextant");

// Authentication — cookie-based session
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "sextant_session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        // Return 401 instead of redirecting — frontend handles the redirect to /login
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

// HTTP clients (ESI + SSO token exchange)
builder.Services.AddHttpClient();

// Memory cache (access token caching)
builder.Services.AddMemoryCache();

// App services
builder.Services.AddScoped<TokenService>();

// OpenAPI + Scalar
builder.Services.AddOpenApi();

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Static files (React frontend) — before auth so assets don't require login
app.UseDefaultFiles();
app.UseStaticFiles();

// Auth middleware
app.UseAuthentication();
app.UseAuthorization();

// OpenAPI + Scalar UI
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "Sextant API";
    options.Theme = ScalarTheme.DeepSpace;
});

// Health check
app.MapHealthChecks("/health").AllowAnonymous();

// Auth endpoints
AuthEndpoints.Map(app);

// SPA fallback — serve index.html for all non-API routes
app.MapFallbackToFile("index.html");

app.Run();
