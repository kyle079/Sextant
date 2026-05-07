using Microsoft.EntityFrameworkCore;
using Sextant.Data;

namespace Sextant.Services;

public class SdeRefreshHostedService(IServiceProvider services, ILogger<SdeRefreshHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RefreshIfEmpty(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Tuesday && DateTime.UtcNow.Hour == 11 && DateTime.UtcNow.Minute >= 15)
                await RefreshIfStale(stoppingToken);
        }
    }

    private async Task RefreshIfEmpty(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (await db.WormholeSystems.AnyAsync(stoppingToken) && await db.WormholeTypes.AnyAsync(stoppingToken))
                return;

            logger.LogInformation("SDE tables are empty; starting initial refresh");
            await scope.ServiceProvider.GetRequiredService<SdeRefreshService>().Refresh(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception ex) { logger.LogError(ex, "Initial SDE refresh failed"); }
    }

    private async Task RefreshIfStale(CancellationToken stoppingToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var latestSuccess = await db.SdeRefreshRuns
            .Where(x => x.State == Models.SdeRefreshState.Succeeded)
            .OrderByDescending(x => x.CompletedAt)
            .FirstOrDefaultAsync(stoppingToken);

        if (latestSuccess?.CompletedAt?.Date == DateTime.UtcNow.Date)
            return;

        logger.LogInformation("Starting scheduled weekly SDE refresh");
        await scope.ServiceProvider.GetRequiredService<SdeRefreshService>().Refresh(stoppingToken);
    }
}
