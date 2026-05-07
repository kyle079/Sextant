using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Sextant.Data;

var builder = WebApplication.CreateBuilder(args);

// Database
var dbPath = builder.Configuration["Database:Path"] ?? "data/sextant.db";
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// OpenAPI + Scalar
builder.Services.AddOpenApi();

// Memory cache (used by ESI handlers later)
builder.Services.AddMemoryCache();

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// OpenAPI + Scalar UI
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "Sextant API";
    options.Theme = ScalarTheme.DeepSpace;
});

// Health check
app.MapHealthChecks("/health");

// Serve React frontend static files
app.UseDefaultFiles();
app.UseStaticFiles();

// SPA fallback: serve index.html for all non-API routes
app.MapFallbackToFile("index.html");

app.Run();
