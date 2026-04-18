using GameShelf.Models.Domain.Entities;
using GameShelf.Models.ViewModels.DealRatings;
using GameShelf.Models.ViewModels.LiveDeals;

namespace GameShelf.Models.ViewModels.GameDeals
{
    /// <summary>
    /// ViewModel for displaying deal details with verdict ratings
    /// </summary>
    public class GameDealDetailsViewModel
    {
        // Deal information
        public string Title { get; set; } = string.Empty;
        public decimal SalePriceUsd { get; set; }
        public decimal NormalPriceUsd { get; set; }
        public decimal SalePriceEur { get; set; }
        public decimal NormalPriceEur { get; set; }
        public decimal SavingsPercent { get; set; }
        public string? ThumbUrl { get; set; }
        public string DealUrl { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string? DealId { get; set; }
        public Guid? CustomDealId { get; set; }
        public Guid GameDealId { get; set; }

        // Rating information
        public DealRatingViewModel Rating { get; set; } = new();
    }
}
