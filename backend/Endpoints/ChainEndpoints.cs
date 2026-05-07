using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sextant.Data;
using Sextant.Models;

namespace Sextant.Endpoints;

public static class ChainEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/chain").WithTags("Chain").RequireAuthorization();
        group.MapGet("/systems", GetSystems);
        group.MapPost("/systems", AddSystem);
        group.MapPut("/systems/{id:int}", UpdateSystem);
        group.MapDelete("/systems/{id:int}", DeleteSystem);
        group.MapGet("/connections", GetConnections);
        group.MapPost("/connections", AddConnection);
        group.MapPut("/connections/{id:int}", UpdateConnection);
        group.MapDelete("/connections/{id:int}", DeleteConnection);
    }

    static async Task<IResult> GetSystems(AppDbContext db, CancellationToken ct)
    {
        var sigCounts = await db.Signatures.GroupBy(x => x.ChainSystemId)
            .Select(x => new { ChainSystemId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.ChainSystemId, x => x.Count, ct);

        var systems = await db.ChainSystems.AsNoTracking().OrderByDescending(x => x.IsHome).ThenBy(x => x.Name).ToListAsync(ct);
        return Results.Ok(systems.Select(x => ToSystemDto(x, sigCounts.GetValueOrDefault(x.Id))));
    }

    static async Task<IResult> AddSystem(CreateChainSystemRequest request, HttpContext ctx, AppDbContext db, CancellationToken ct)
    {
        var sde = await db.WormholeSystems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == request.Name || x.SolarSystemId == request.SolarSystemId, ct);
        if (sde is null && string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest("System name is required.");

        var now = DateTime.UtcNow;
        var system = new ChainSystem
        {
            SolarSystemId = request.SolarSystemId != 0 ? request.SolarSystemId : sde?.SolarSystemId ?? 0,
            Name = !string.IsNullOrWhiteSpace(request.Name) ? request.Name.Trim().ToUpperInvariant() : sde!.Name,
            Class = sde?.Class ?? request.Class ?? string.Empty,
            ClassNumber = sde?.ClassNumber ?? request.ClassNumber,
            Effect = sde?.Effect ?? request.Effect,
            Statics = sde?.Statics ?? string.Join(',', request.Statics ?? []),
            Notes = request.Notes ?? string.Empty,
            IsHome = request.IsHome || !await db.ChainSystems.AnyAsync(ct),
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.ChainSystems.Add(system);
        await db.SaveChangesAsync(ct);
        await LogActivity(db, ctx, ActivityEventType.Chain, $"Added {system.Name} to chain", new { system.Id, system.Name }, system.SolarSystemId, ct);
        return Results.Created($"/chain/systems/{system.Id}", ToSystemDto(system, 0));
    }

    static async Task<IResult> UpdateSystem(int id, UpdateChainSystemRequest request, HttpContext ctx, AppDbContext db, CancellationToken ct)
    {
        var system = await db.ChainSystems.FindAsync([id], ct);
        if (system is null) return Results.NotFound();

        system.Notes = request.Notes ?? system.Notes;
        system.IsHome = request.IsHome ?? system.IsHome;
        system.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await LogActivity(db, ctx, ActivityEventType.Chain, $"Updated {system.Name}", new { system.Id, system.Notes, system.IsHome }, system.SolarSystemId, ct);
        return Results.Ok(ToSystemDto(system, await db.Signatures.CountAsync(x => x.ChainSystemId == id, ct)));
    }

    static async Task<IResult> DeleteSystem(int id, HttpContext ctx, AppDbContext db, CancellationToken ct)
    {
        var system = await db.ChainSystems.FindAsync([id], ct);
        if (system is null) return Results.NotFound();

        var now = DateTime.UtcNow;
        system.DeletedAt = now;
        foreach (var connection in await db.ChainConnections.Where(x => x.SourceSystemId == id || x.TargetSystemId == id).ToListAsync(ct))
            connection.DeletedAt = now;
        foreach (var sig in await db.Signatures.Where(x => x.ChainSystemId == id).ToListAsync(ct))
            sig.DeletedAt = now;

        await db.SaveChangesAsync(ct);
        await LogActivity(db, ctx, ActivityEventType.Chain, $"Removed {system.Name} from chain", new { system.Id, system.Name }, system.SolarSystemId, ct);
        return Results.NoContent();
    }

    static async Task<IResult> GetConnections(AppDbContext db, CancellationToken ct)
    {
        var connections = await db.ChainConnections.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct);
        return Results.Ok(connections.Select(ToConnectionDto));
    }

    static async Task<IResult> AddConnection(CreateChainConnectionRequest request, HttpContext ctx, AppDbContext db, CancellationToken ct)
    {
        if (!await db.ChainSystems.AnyAsync(x => x.Id == request.SourceSystemId, ct) ||
            !await db.ChainSystems.AnyAsync(x => x.Id == request.TargetSystemId, ct))
            return Results.BadRequest("Source and destination systems must exist.");

        var wh = string.IsNullOrWhiteSpace(request.WormholeTypeCode)
            ? null
            : await db.WormholeTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Code == request.WormholeTypeCode.Trim().ToUpperInvariant(), ct);
        var scannedAt = request.ScannedAt?.ToUniversalTime() ?? DateTime.UtcNow;
        var lifetime = wh?.LifetimeHours ?? request.LifetimeHours;
        var connection = new ChainConnection
        {
            SourceSystemId = request.SourceSystemId,
            TargetSystemId = request.TargetSystemId,
            WormholeTypeCode = wh?.Code ?? request.WormholeTypeCode?.Trim().ToUpperInvariant() ?? string.Empty,
            Status = request.Status ?? "Stable",
            MassStatus = request.MassStatus ?? "Fresh",
            MaxMassKg = wh?.MaxMassKg ?? request.MaxMassKg,
            MaxJumpMassKg = wh?.MaxJumpMassKg ?? request.MaxJumpMassKg,
            LifetimeHours = lifetime,
            ScannedAt = scannedAt,
            EolAt = lifetime.HasValue ? scannedAt.AddHours(lifetime.Value) : null,
            SignatureId = request.SignatureId,
            Notes = request.Notes ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.ChainConnections.Add(connection);
        await db.SaveChangesAsync(ct);
        await LogActivity(db, ctx, ActivityEventType.Chain, $"Added {connection.WormholeTypeCode} connection", new { connection.Id, connection.SourceSystemId, connection.TargetSystemId }, null, ct);
        return Results.Created($"/chain/connections/{connection.Id}", ToConnectionDto(connection));
    }

    static async Task<IResult> UpdateConnection(int id, UpdateChainConnectionRequest request, HttpContext ctx, AppDbContext db, CancellationToken ct)
    {
        var connection = await db.ChainConnections.FindAsync([id], ct);
        if (connection is null) return Results.NotFound();

        if (!string.IsNullOrWhiteSpace(request.WormholeTypeCode))
        {
            var code = request.WormholeTypeCode.Trim().ToUpperInvariant();
            var wh = await db.WormholeTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code, ct);
            connection.WormholeTypeCode = wh?.Code ?? code;
            connection.MaxMassKg = wh?.MaxMassKg ?? connection.MaxMassKg;
            connection.MaxJumpMassKg = wh?.MaxJumpMassKg ?? connection.MaxJumpMassKg;
            connection.LifetimeHours = wh?.LifetimeHours ?? connection.LifetimeHours;
            connection.EolAt = connection.LifetimeHours.HasValue ? connection.ScannedAt.AddHours(connection.LifetimeHours.Value) : connection.EolAt;
        }

        connection.Status = request.Status ?? connection.Status;
        connection.MassStatus = request.MassStatus ?? connection.MassStatus;
        connection.Notes = request.Notes ?? connection.Notes;
        connection.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await LogActivity(db, ctx, ActivityEventType.Chain, $"Updated connection {connection.Id}", new { connection.Id }, null, ct);
        return Results.Ok(ToConnectionDto(connection));
    }

    static async Task<IResult> DeleteConnection(int id, HttpContext ctx, AppDbContext db, CancellationToken ct)
    {
        var connection = await db.ChainConnections.FindAsync([id], ct);
        if (connection is null) return Results.NotFound();

        connection.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await LogActivity(db, ctx, ActivityEventType.Chain, $"Collapsed connection {id}", new { id }, null, ct);
        return Results.NoContent();
    }

    internal static async Task LogActivity(AppDbContext db, HttpContext ctx, ActivityEventType type, string summary, object detail, long? systemId, CancellationToken ct)
    {
        db.ActivityEvents.Add(new ActivityEvent
        {
            Type = type,
            CharacterId = long.TryParse(ctx.User.FindFirstValue("activeCharacterId"), out var characterId) ? characterId : 0,
            CharacterName = ctx.User.FindFirstValue(ClaimTypes.Name) ?? "Unknown",
            Summary = summary,
            DetailJson = JsonSerializer.Serialize(detail),
            SystemId = systemId,
            Timestamp = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }

    static object ToSystemDto(ChainSystem x, int sigCount) => new
    {
        x.Id,
        x.SolarSystemId,
        x.Name,
        x.Class,
        x.ClassNumber,
        x.Effect,
        statics = x.Statics == string.Empty ? Array.Empty<string>() : x.Statics.Split(',', StringSplitOptions.RemoveEmptyEntries),
        x.Notes,
        x.IsHome,
        x.CreatedAt,
        x.UpdatedAt,
        SigCount = sigCount,
    };

    static object ToConnectionDto(ChainConnection x) => new
    {
        x.Id,
        x.SourceSystemId,
        x.TargetSystemId,
        x.WormholeTypeCode,
        x.Status,
        x.MassStatus,
        x.MaxMassKg,
        x.MaxJumpMassKg,
        x.LifetimeHours,
        x.ScannedAt,
        x.EolAt,
        x.SignatureId,
        x.Notes,
        x.CreatedAt,
        x.UpdatedAt,
    };

    public record CreateChainSystemRequest(long SolarSystemId, string Name, string? Class, int? ClassNumber, string? Effect, string[]? Statics, string? Notes, bool IsHome);
    public record UpdateChainSystemRequest(string? Notes, bool? IsHome);
    public record CreateChainConnectionRequest(int SourceSystemId, int TargetSystemId, string? WormholeTypeCode, string? Status, string? MassStatus, long? MaxMassKg, long? MaxJumpMassKg, int? LifetimeHours, DateTime? ScannedAt, string? SignatureId, string? Notes);
    public record UpdateChainConnectionRequest(string? WormholeTypeCode, string? Status, string? MassStatus, string? Notes);
}
