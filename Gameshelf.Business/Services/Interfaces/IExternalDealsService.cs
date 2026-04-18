using GameShelf.Models.ViewModels.LiveDeals;

namespace GameShelf.Business.Services.Interfaces
{
    /// <summary>
    /// Fetches live game deals from external APIs (e.g. CheapShark) for a given platform.
    /// </summary>
    public interface IExternalDealsService
    {
        /// <summary>
        /// Gets current discounted deals for the given platform with pagination.
        /// </summary>
        Task<IEnumerable<LiveDealDto>> GetLiveDealsAsync(string platformName, int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Gets current discounted deals from ALL supported platforms with pagination.
        /// </summary>
        Task<IEnumerable<LiveDealDto>> GetLiveDealsAllPlatformsAsync(int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Gets total count of deals for a platform (for pagination).
        /// </summary>
        int GetTotalDealsCount(string platformName);

        /// <summary>
        /// Gets total count of all deals across platforms (for pagination).
        /// </summary>
        int GetTotalAllDealsCount();

        /// <summary>
        /// Gets featured deals (with verdict ratings) sorted by community verdict percentages, with pagination.
        /// </summary>
        Task<IEnumerable<LiveDealDto>> GetFeaturedDealsAsync(int pageNumber = 1, int pageSize = 40);
    }
}
