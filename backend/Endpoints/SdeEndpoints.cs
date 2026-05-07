using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Sextant.Data;
using Sextant.Services;

namespace Sextant.Endpoints;

public static class SdeEndpoints
{
    public static void Map(WebApplication app)
    {
        var admin = app.MapGroup("/admin/sde").WithTags("SDE").RequireAuthorization();
        admin.MapPost("/refresh", Refresh).WithName("RefreshSde");
        admin.MapGet("/status", Status).WithName("GetSdeStatus");

        var sde = app.MapGroup("/sde").WithTags("SDE").RequireAuthorization();
        sde.MapGet("/systems", SearchSystems).WithName("SearchWormholeSystems");
        sde.MapGet("/wormhole-types", SearchWormholeTypes).WithName("SearchWormholeTypes");
    }

    static async Task<IResult> Refresh(HttpContext ctx, SdeRefreshService sdeRefreshService, CancellationToken cancellationToken)
    {
        if (!IsAdmin(ctx)) return Results.Forbid();
        return Results.Ok(await sdeRefreshService.Refresh(cancellationToken));
    }

    static async Task<IResult> Status(SdeRefreshService sdeRefreshService, CancellationToken cancellationToken) =>
        Results.Ok(await sdeRefreshService.GetStatus(cancellationToken));

    static async Task<IResult> SearchSystems(string? q, AppDbContext db, CancellationToken cancellationToken)
    {
        var query = db.WormholeSystems.AsNoTracking().OrderBy(x => x.Name).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => x.Name.Contains(term) || (x.SecondaryName != null && x.SecondaryName.Contains(term)));
        }

        var systems = await query.Take(25).ToListAsync(cancellationToken);
        return Results.Ok(systems.Select(x => new
        {
            x.Id,
            x.SolarSystemId,
            x.Name,
            x.SecondaryName,
            x.Class,
            x.ClassNumber,
            x.Effect,
            statics = x.Statics == "" ? Array.Empty<string>() : x.Statics.Split(',', StringSplitOptions.RemoveEmptyEntries),
            x.IsShattered,
        }));
    }

    static async Task<IResult> SearchWormholeTypes(string? q, AppDbContext db, CancellationToken cancellationToken)
    {
        var query = db.WormholeTypes.AsNoTracking().OrderBy(x => x.Code).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => x.Code.Contains(term));
        }

        return Results.Ok(await query.Take(100).ToListAsync(cancellationToken));
    }

    private static bool IsAdmin(HttpContext ctx) =>
        string.Equals(ctx.User.FindFirstValue("role"), "Admin", StringComparison.OrdinalIgnoreCase);
}
