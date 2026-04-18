using GameShelf.Models.Domain.Entities;

namespace GameShelf.Models.ViewModels.DealRatings
{
    /// <summary>
    /// ViewModel for displaying a single deal review
    /// </summary>
    public class DealReviewViewModel
    {
        public Guid Id { get; set; }
        public DealVerdict Verdict { get; set; }
        public int ReasonId { get; set; }
        public string ReasonText { get; set; } = string.Empty;
        public string? ReviewText { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
