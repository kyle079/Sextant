using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sextant.Data;
using Sextant.Models;

namespace Sextant.Services;

public record SdeStatusResponse(
    DateTime? LastRefreshStartedAt,
    DateTime? LastRefreshCompletedAt,
    string State,
    int WormholeSystemCount,
    int WormholeTypeCount,
    string? Error);

public class SdeRefreshService(
    AppDbContext db,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<SdeRefreshService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<SdeStatusResponse> GetStatus(CancellationToken cancellationToken = default)
    {
        var latest = await db.SdeRefreshRuns
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new SdeStatusResponse(
            latest?.StartedAt,
            latest?.CompletedAt,
            latest?.State.ToString() ?? "NeverRun",
            await db.WormholeSystems.CountAsync(cancellationToken),
            await db.WormholeTypes.CountAsync(cancellationToken),
            latest?.Error);
    }

    public async Task<SdeStatusResponse> Refresh(CancellationToken cancellationToken = default)
    {
        var run = new SdeRefreshRun { StartedAt = DateTime.UtcNow };
        db.SdeRefreshRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var systems = await FetchJson<List<EveDataSystem>>(SystemsDataUrl, cancellationToken);
            var wormholes = await FetchJson<List<EveDataWormhole>>(WormholeDataUrl, cancellationToken);
            var scanStrengths = await FetchScanStrengths(cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var source in systems.Where(x => x.WhClass.HasValue))
            {
                var whClass = source.WhClass.GetValueOrDefault();
                var existing = await db.WormholeSystems
                    .FirstOrDefaultAsync(x => x.SolarSystemId == source.Id, cancellationToken);
                existing ??= new WormholeSystem { SolarSystemId = source.Id };

                existing.Name = source.Name;
                existing.SecondaryName = source.SecondaryName;
                existing.ClassNumber = whClass;
                existing.Class = ClassName(whClass);
                existing.Effect = string.IsNullOrWhiteSpace(source.Effect?.Name) ? null : source.Effect!.Name;
                existing.Statics = string.Join(",", source.StaticConnections ?? []);
                existing.IsShattered = string.Equals(source.SecurityClass, "SHATTERED", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(source.SecurityClass, "SHATTERED-WH", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(source.SecondaryName, "Shattered", StringComparison.OrdinalIgnoreCase);
                existing.UpdatedAt = now;

                if (existing.Id == 0) db.WormholeSystems.Add(existing);
            }

            foreach (var source in wormholes)
            {
                var existing = await db.WormholeTypes
                    .FirstOrDefaultAsync(x => x.Code == source.Type, cancellationToken);
                existing ??= new WormholeType { Code = source.Type };

                existing.DestinationType = source.Destination?.Type;
                existing.DestinationClass = source.Destination?.WhClass;
                existing.LifetimeHours = (int)source.LifetimeHrs;
                existing.MaxMassKg = source.Mass?.Total;
                existing.MaxJumpMassKg = source.Mass?.Jump;
                existing.ScanStrength = scanStrengths.GetValueOrDefault(source.Type);
                existing.UpdatedAt = now;

                if (existing.Id == 0) db.WormholeTypes.Add(existing);
            }

            run.State = SdeRefreshState.Succeeded;
            run.CompletedAt = DateTime.UtcNow;
            run.WormholeSystemCount = await db.WormholeSystems.CountAsync(cancellationToken);
            run.WormholeTypeCount = await db.WormholeTypes.CountAsync(cancellationToken);

            db.ActivityEvents.Add(new ActivityEvent
            {
                Type = ActivityEventType.System,
                CharacterName = "Sextant",
                Summary = $"SDE refresh completed: {run.WormholeSystemCount} systems, {run.WormholeTypeCount} wormhole types",
                Timestamp = DateTime.UtcNow,
                DetailJson = JsonSerializer.Serialize(new { run.WormholeSystemCount, run.WormholeTypeCount }),
            });

            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("SDE refresh completed with {Systems} wormhole systems and {Types} wormhole types", run.WormholeSystemCount, run.WormholeTypeCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SDE refresh failed");
            run.State = SdeRefreshState.Failed;
            run.CompletedAt = DateTime.UtcNow;
            run.Error = ex.Message;
            await db.SaveChangesAsync(CancellationToken.None);
        }

        return await GetStatus(cancellationToken);
    }

    private string SystemsDataUrl => config["SDE:SystemsUrl"] ??
        "https://unpkg.com/@eve-data/systems@3.1.0/lib/assets/systems.json";

    private string WormholeDataUrl => config["SDE:WormholesUrl"] ??
        "https://unpkg.com/@eve-data/wormholes@0.0.9/lib/assets/wormholes.json";

    private string PathfinderWormholesUrl => config["SDE:PathfinderWormholesUrl"] ??
        "https://raw.githubusercontent.com/exodus4d/pathfinder/master/export/csv/wormhole.csv";

    private async Task<T> FetchJson<T>(string url, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("sde");
        await using var stream = await client.GetStreamAsync(url, cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken) ??
            throw new InvalidOperationException($"Unable to parse SDE JSON from {url}");
    }

    private async Task<Dictionary<string, decimal?>> FetchScanStrengths(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("sde");
        var csv = await client.GetStringAsync(PathfinderWormholesUrl, cancellationToken);
        var strengths = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in csv.Split('\n').Skip(1))
        {
            var cells = line.Trim().Split(';');
            if (cells.Length < 3 || string.IsNullOrWhiteSpace(cells[1])) continue;
            strengths[cells[1]] = decimal.TryParse(cells[2], System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : null;
        }

        return strengths;
    }

    private static string ClassName(int whClass) => whClass switch
    {
        >= 1 and <= 6 => $"C{whClass}",
        7 => "High Sec",
        8 => "Low Sec",
        9 => "Null Sec",
        12 => "Thera",
        13 => "Shattered",
        _ => $"Class {whClass}",
    };

    private sealed record EveDataSystem(
        string Name,
        string? SecondaryName,
        long Id,
        string? SecurityClass,
        EveDataEffect? Effect,
        int? WhClass,
        List<string>? StaticConnections);

    private sealed record EveDataEffect(string? Name);

    private sealed record EveDataWormhole(
        string Type,
        double LifetimeHrs,
        EveDataEndpoint? Destination,
        EveDataMass? Mass);

    private sealed record EveDataEndpoint(string Type, int? WhClass);
    private sealed record EveDataMass(long? Total, long? Jump);
}
