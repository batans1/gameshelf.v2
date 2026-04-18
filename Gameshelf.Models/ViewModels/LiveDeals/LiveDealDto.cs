namespace GameShelf.Models.ViewModels.LiveDeals
{
    /// <summary>
    /// DTO for a single live (external) game deal from CheapShark or similar.
    /// Prices in USD (API) and EUR (converted for display).
    /// </summary>
    public class LiveDealDto
    {
        public string Title { get; set; } = string.Empty;
        /// <summary>Sale price in USD.</summary>
        public decimal SalePriceUsd { get; set; }
        /// <summary>Normal price in USD.</summary>
        public decimal NormalPriceUsd { get; set; }
        /// <summary>Sale price in EUR (converted from USD).</summary>
        public decimal SalePriceEur { get; set; }
        /// <summary>Normal price in EUR (converted from USD).</summary>
        public decimal NormalPriceEur { get; set; }
        public decimal SavingsPercent { get; set; }
        public string? ThumbUrl { get; set; }
        public string DealUrl { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string? DealId { get; set; }
        /// <summary>Custom deal ID (Guid) for manually added deals.</summary>
        public Guid? CustomDealId { get; set; }
        /// <summary>Average rating (1-5 stars) for this deal.</summary>
        public double AverageRating { get; set; }
        /// <summary>User's rating for this deal (if logged in).</summary>
        public int? UserRating { get; set; }
        /// <summary>Total number of ratings.</summary>
        public int RatingCount { get; set; }
        /// <summary>Community verdict: % of ratings that are Buy Now.</summary>
        public double BuyNowPercent { get; set; }
        /// <summary>Community verdict: % of ratings that are Wait.</summary>
        public double WaitPercent { get; set; }
        /// <summary>Community verdict: % of ratings that are Not Worth It.</summary>
        public double NotWorthItPercent { get; set; }
    }
}
