using GameShelf.Models.Domain.Entities;
using GameShelf.Models.ViewModels.DealRatings;
using GameShelf.Models.ViewModels.LiveDeals;
using GameShelf.Business.Services.Moderation;

namespace GameShelf.Business.Services.Interfaces
{
    /// <summary>
    /// Service for managing deal verdict ratings (Buy Now, Wait, Not Worth It)
    /// </summary>
    public interface IDealRatingService
    {
        /// <summary>
        /// Gets the user's rating for a deal (live or custom)
        /// </summary>
        Task<DealRatingViewModel?> GetUserRatingAsync(string? dealId, Guid? customDealId, string userId);

        /// <summary>
        /// Gets the community verdict statistics for a deal
        /// </summary>
        Task<CommunityVerdictViewModel> GetCommunityVerdictAsync(string? dealId, Guid? customDealId);

        /// <summary>
        /// Sets or updates a user's rating for a deal
        /// </summary>
        Task<ModerationOutcome?> SetRatingAsync(string? dealId, Guid? customDealId, string storeName, string userId, DealVerdict verdict, int reasonId, string? reviewText);

        /// <summary>
        /// Gets all reviews for a deal (with review text)
        /// </summary>
        Task<IEnumerable<DealReviewViewModel>> GetDealReviewsAsync(string? dealId, Guid? customDealId, DealVerdict? verdictFilter = null);

        /// <summary>
        /// Gets the total rating count for a deal
        /// </summary>
        Task<int> GetRatingCountAsync(string? dealId, Guid? customDealId);
        Task<IEnumerable<DealReviewViewModel>> GetUserReviewsAsync(string userId, bool includeWithoutText);
        Task DeleteReviewTextAsync(Guid ratingId);
    }
}
