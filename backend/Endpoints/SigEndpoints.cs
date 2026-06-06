using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Sextant.Data;
using Sextant.Models;

namespace Sextant.Endpoints;

public static class SigEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/sigs").WithTags("Signatures").RequireAuthorization();
        group.MapGet("/{systemId:int}", GetSigs);
        group.MapPost("/{systemId:int}", AddSig);
        group.MapPost("/{systemId:int}/paste", PasteSigs);
        group.MapPut("/{id:int}", UpdateSig);
        group.MapDelete("/{id:int}", DeleteSig);
    }

    static async Task<IResult> GetSigs(int systemId, AppDbContext db, CancellationToken ct)
    {
        if (!await db.ChainSystems.AnyAsync(x => x.Id == systemId, ct)) return Results.NotFound();
        var sigs = await db.Signatures.AsNoTracking()
            .Where(x => x.ChainSystemId == systemId)
            .OrderBy(x => x.SignatureId)
            .ToListAsync(ct);
        return Results.Ok(sigs.Select(ToDto));
    }

    static async Task<IResult> AddSig(int systemId, UpsertSignatureRequest request, HttpContext ctx, AppDbContext db, CancellationToken ct)
    {
        if (!await db.ChainSystems.AnyAsync(x => x.Id == systemId, ct)) return Results.NotFound();
        var sig = new Signature { ChainSystemId = systemId };
        Apply(sig, request, ctx);
        db.Signatures.Add(sig);
        await db.SaveChangesAsync(ct);
        await ChainEndpoints.LogActivity(db, ctx, ActivityEventType.Sig, $"Added sig {sig.SignatureId}", new { sig.Id, systemId }, systemId, ct);
        return Results.Created($"/sigs/{systemId}/{sig.Id}", ToDto(sig));
    }

    static async Task<IResult> PasteSigs(int systemId, PasteSignaturesRequest request, HttpContext ctx, AppDbContext db, CancellationToken ct)
    {
        if (!await db.ChainSystems.AnyAsync(x => x.Id == systemId, ct)) return Results.NotFound();
        var parsed = ParseClipboard(request.Text).ToList();
        var existing = await db.Signatures.Where(x => x.ChainSystemId == systemId).ToDictionaryAsync(x => x.SignatureId, ct);
        var added = 0;
        var updated = 0;

        foreach (var item in parsed)
        {
            if (existing.TryGetValue(item.SignatureId, out var sig))
            {
                Apply(sig, item, ctx);
                updated++;
            }
            else
            {
                sig = new Signature { ChainSystemId = systemId };
                Apply(sig, item, ctx);
                db.Signatures.Add(sig);
                added++;
            }
        }

        await db.SaveChangesAsync(ct);
        await ChainEndpoints.LogActivity(db, ctx, ActivityEventType.Sig, $"Imported {parsed.Count} signatures", new { systemId, added, updated }, systemId, ct);
        var sigs = await db.Signatures.AsNoTracking().Where(x => x.ChainSystemId == systemId).OrderBy(x => x.SignatureId).ToListAsync(ct);
        return Results.Ok(new { added, updated, signatures = sigs.Select(ToDto) });
    }

    static async Task<IResult> UpdateSig(int id, UpsertSignatureRequest request, HttpContext ctx, AppDbContext db, CancellationToken ct)
    {
        var sig = await db.Signatures.FindAsync([id], ct);
        if (sig is null) return Results.NotFound();
        Apply(sig, request, ctx);
        await db.SaveChangesAsync(ct);
        await ChainEndpoints.LogActivity(db, ctx, ActivityEventType.Sig, $"Updated sig {sig.SignatureId}", new { sig.Id, sig.ChainSystemId }, sig.ChainSystemId, ct);
        return Results.Ok(ToDto(sig));
    }

    static async Task<IResult> DeleteSig(int id, HttpContext ctx, AppDbContext db, CancellationToken ct)
    {
        var sig = await db.Signatures.FindAsync([id], ct);
        if (sig is null) return Results.NotFound();
        sig.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await ChainEndpoints.LogActivity(db, ctx, ActivityEventType.Sig, $"Cleared sig {sig.SignatureId}", new { sig.Id, sig.ChainSystemId }, sig.ChainSystemId, ct);
        return Results.NoContent();
    }

    static void Apply(Signature sig, UpsertSignatureRequest request, HttpContext ctx)
    {
        sig.SignatureId = request.SignatureId.Trim().ToUpperInvariant();
        sig.Group = request.Group?.Trim() ?? string.Empty;
        sig.Type = string.IsNullOrWhiteSpace(request.Type) ? "Unknown" : request.Type.Trim();
        sig.Name = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim();
        sig.ScanPercentage = Math.Clamp(request.ScanPercentage, 0, 100);
        sig.EolAt = request.EolAt?.ToUniversalTime();
        sig.ScannedByCharacterId = long.TryParse(ctx.User.FindFirst("activeCharacterId")?.Value, out var characterId) ? characterId : 0;
        sig.ScannedByCharacterName = ctx.User.Identity?.Name ?? "Unknown";
        sig.UpdatedAt = DateTime.UtcNow;
        if (sig.CreatedAt == default) sig.CreatedAt = DateTime.UtcNow;
    }

    static IEnumerable<UpsertSignatureRequest> ParseClipboard(string text)
    {
        foreach (var rawLine in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var cols = rawLine.Trim().Split('\t');
            if (cols.Length < 3) continue;
            var signatureId = cols[0].Trim();
            var group = cols.ElementAtOrDefault(1)?.Trim() ?? string.Empty;
            var typeName = cols.ElementAtOrDefault(2)?.Trim() ?? string.Empty;
            var name = cols.ElementAtOrDefault(3)?.Trim();
            var scanRaw = cols.ElementAtOrDefault(4)?.Trim().TrimEnd('%') ?? "0";
            if (string.IsNullOrWhiteSpace(signatureId) || group.Contains("anomaly", StringComparison.OrdinalIgnoreCase)) continue;
            decimal.TryParse(scanRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var scan);
            yield return new UpsertSignatureRequest(signatureId, group, MapType(typeName, name ?? string.Empty), string.IsNullOrWhiteSpace(name) ? null : name, scan, null);
        }
    }

    static string MapType(string group, string name)
    {
        var text = $"{group} {name}".ToLowerInvariant();
        if (text.Contains("wormhole") || text.Contains("unstable")) return "Wormhole";
        if (text.Contains("data")) return "Data";
        if (text.Contains("relic") || text.Contains("ruins")) return "Relic";
        if (text.Contains("gas") || text.Contains("cloud")) return "Gas";
        if (text.Contains("combat") || text.Contains("site") || text.Contains("outpost") || text.Contains("hub")) return "Combat";
        return "Unknown";
    }

    static object ToDto(Signature x) => new
    {
        x.Id,
        x.ChainSystemId,
        x.SignatureId,
        x.Group,
        x.Type,
        x.Name,
        x.ScanPercentage,
        x.ScannedByCharacterId,
        x.ScannedByCharacterName,
        x.EolAt,
        x.CreatedAt,
        x.UpdatedAt,
    };

    public record UpsertSignatureRequest(string SignatureId, string? Group, string Type, string? Name, decimal ScanPercentage, DateTime? EolAt);
    public record PasteSignaturesRequest(string Text);
}
