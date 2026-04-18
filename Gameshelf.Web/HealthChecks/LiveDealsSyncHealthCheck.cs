using GameShelf.Data.Persistance;
using GameShelf.Models.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GameShelf.Web.HealthChecks;

/// <summary>
/// Health check for live deals sync: verifies we have live deals in DB and optionally recent sync.
/// </summary>
public class LiveDealsSyncHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;

    public LiveDealsSyncHealthCheck(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var count = await db.GameDeals.CountAsync(d => d.Source == DealSource.Live && d.IsActive && d.IsAvailable, cancellationToken);
            var lastSync = await db.GameDeals
                .Where(d => d.Source == DealSource.Live && d.LastSyncedAt != null)
                .OrderByDescending(d => d.LastSyncedAt)
                .Select(d => d.LastSyncedAt)
                .FirstOrDefaultAsync(cancellationToken);

        if (count > 0)
            {
                var msg = lastSync.HasValue
                    ? $"Live deals in DB: {count}, last synced: {lastSync:u}"
                    : $"Live deals in DB: {count}";
                return HealthCheckResult.Healthy(msg);
            }

            return HealthCheckResult.Degraded("No live deals in database yet (sync runs on startup and every hour)");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Live deals sync health check failed", ex);
        }
    }
}
