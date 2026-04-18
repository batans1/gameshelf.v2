using GameShelf.Models.Domain.Entities;

namespace GameShelf.Models.ViewModels.DealRatings
{
    /// <summary>
    /// ViewModel for displaying a user's deal rating
    /// </summary>
    public class DealRatingViewModel
    {
        public DealVerdict? UserVerdict { get; set; }
        public int? UserReasonId { get; set; }
        public string? UserReviewText { get; set; }
        public CommunityVerdictViewModel CommunityVerdict { get; set; } = new();
    }
}
