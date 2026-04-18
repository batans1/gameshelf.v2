namespace GameShelf.Models.Domain.Entities
{
    /// <summary>
    /// Constants for deal rating reasons. Each verdict has 4 reasons.
    /// </summary>
    public static class DealRatingReason
    {
        public const int BuyNow_PriceNearHistoricalLow = 1;
        public const int BuyNow_RarelyDiscounted = 2;
        public const int BuyNow_HighValueForPrice = 3;
        public const int BuyNow_StrongCommunityConfidence = 4;

        public const int Wait_LikelyBiggerDiscountSoon = 5;
        public const int Wait_FrequentlyDiscounted = 6;
        public const int Wait_PriceAboveHistoricalAverage = 7;
        public const int Wait_SaleJustStarted = 8;

        public const int NotWorthIt_LowQualityPoorReviews = 9;
        public const int NotWorthIt_DiscountTooSmall = 10;
        public const int NotWorthIt_TechnicalPerformanceIssues = 11;
        public const int NotWorthIt_OverhypedNotWorthPrice = 12;

        /// <summary>
        /// Gets the reason text for a given reason ID.
        /// </summary>
        public static string GetReasonText(int reasonId)
        {
            return reasonId switch
            {
                BuyNow_PriceNearHistoricalLow => "Price is near Historical Low",
                BuyNow_RarelyDiscounted => "Rarely Discounted",
                BuyNow_HighValueForPrice => "High Value for Price",
                BuyNow_StrongCommunityConfidence => "Strong Community Confidence",
                Wait_LikelyBiggerDiscountSoon => "Likely Bigger Discount Soon",
                Wait_FrequentlyDiscounted => "Frequently Discounted Title",
                Wait_PriceAboveHistoricalAverage => "Price Above Historical Average",
                Wait_SaleJustStarted => "Sale Just Started (Watch It)",
                NotWorthIt_LowQualityPoorReviews => "Low Quality / Poor Reviews of Game",
                NotWorthIt_DiscountTooSmall => "Discount Too Small",
                NotWorthIt_TechnicalPerformanceIssues => "Technical / Performance Issues",
                NotWorthIt_OverhypedNotWorthPrice => "Overhyped / Not Worth the Price",
                _ => "Unknown Reason"
            };
        }

        /// <summary>
        /// Gets all reasons for a given verdict.
        /// </summary>
        public static Dictionary<int, string> GetReasonsForVerdict(DealVerdict verdict)
        {
            return verdict switch
            {
                DealVerdict.BuyNow => new Dictionary<int, string>
                {
                    { BuyNow_PriceNearHistoricalLow, GetReasonText(BuyNow_PriceNearHistoricalLow) },
                    { BuyNow_RarelyDiscounted, GetReasonText(BuyNow_RarelyDiscounted) },
                    { BuyNow_HighValueForPrice, GetReasonText(BuyNow_HighValueForPrice) },
                    { BuyNow_StrongCommunityConfidence, GetReasonText(BuyNow_StrongCommunityConfidence) }
                },
                DealVerdict.Wait => new Dictionary<int, string>
                {
                    { Wait_LikelyBiggerDiscountSoon, GetReasonText(Wait_LikelyBiggerDiscountSoon) },
                    { Wait_FrequentlyDiscounted, GetReasonText(Wait_FrequentlyDiscounted) },
                    { Wait_PriceAboveHistoricalAverage, GetReasonText(Wait_PriceAboveHistoricalAverage) },
                    { Wait_SaleJustStarted, GetReasonText(Wait_SaleJustStarted) }
                },
                DealVerdict.NotWorthIt => new Dictionary<int, string>
                {
                    { NotWorthIt_LowQualityPoorReviews, GetReasonText(NotWorthIt_LowQualityPoorReviews) },
                    { NotWorthIt_DiscountTooSmall, GetReasonText(NotWorthIt_DiscountTooSmall) },
                    { NotWorthIt_TechnicalPerformanceIssues, GetReasonText(NotWorthIt_TechnicalPerformanceIssues) },
                    { NotWorthIt_OverhypedNotWorthPrice, GetReasonText(NotWorthIt_OverhypedNotWorthPrice) }
                },
                _ => new Dictionary<int, string>()
            };
        }

        /// <summary>
        /// Validates if a reason ID is valid for a given verdict.
        /// </summary>
        public static bool IsValidReasonForVerdict(int reasonId, DealVerdict verdict)
        {
            return verdict switch
            {
                DealVerdict.BuyNow => reasonId >= BuyNow_PriceNearHistoricalLow && reasonId <= BuyNow_StrongCommunityConfidence,
                DealVerdict.Wait => reasonId >= Wait_LikelyBiggerDiscountSoon && reasonId <= Wait_SaleJustStarted,
                DealVerdict.NotWorthIt => reasonId >= NotWorthIt_LowQualityPoorReviews && reasonId <= NotWorthIt_OverhypedNotWorthPrice,
                _ => false
            };
        }
    }
}
