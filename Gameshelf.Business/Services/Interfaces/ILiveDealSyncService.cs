namespace GameShelf.Business.Services.Interfaces
{
    /// <summary>
    /// Service for syncing live deals from external API to database
    /// </summary>
    public interface ILiveDealSyncService
    {
        /// <summary>
        /// Syncs live deals for a specific platform/store
        /// </summary>
        Task SyncLiveDealsForPlatformAsync(string platformName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Syncs live deals for all supported platforms
        /// </summary>
        Task SyncAllLiveDealsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets or creates a GameDeal for a live deal by DealId
        /// </summary>
        Task<Guid> GetOrCreateGameDealIdAsync(string dealId, string storeName, CancellationToken cancellationToken = default);
    }
}
