using GameShelf.Business.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameShelf.Business.Services.Implementations
{
    
    
    /// on startup fetches live deals from API into DB, then syncs every 1 hour. Removes expired deals
   
    
    public class LiveDealSyncBackgroundService : BackgroundService
    {
        private const int SyncIntervalHours = 1;
        private const int StartupDelaySeconds = 3; // Short delay after app start, then first sync

        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<LiveDealSyncBackgroundService> _logger;

        public LiveDealSyncBackgroundService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<LiveDealSyncBackgroundService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LiveDealSyncBackgroundService started. Initial sync after {Sec}s, then every {Hours}h.", StartupDelaySeconds, SyncIntervalHours);

            await Task.Delay(TimeSpan.FromSeconds(StartupDelaySeconds), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var syncService = scope.ServiceProvider.GetRequiredService<ILiveDealSyncService>();
                        _logger.LogInformation("Starting live deals sync to database...");
                        await syncService.SyncAllLiveDealsAsync(stoppingToken);
                        _logger.LogInformation("Live deals sync completed successfully.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during live deals sync");
                }

                await Task.Delay(TimeSpan.FromHours(SyncIntervalHours), stoppingToken);
            }
        }
    }
}
