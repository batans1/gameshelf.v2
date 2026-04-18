using GameShelf.Models.Domain.Entities;
using Xunit;

namespace GameShelf.Tests;

public class DealRatingReasonTests
{
    [Theory]
    [InlineData(DealRatingReason.BuyNow_PriceNearHistoricalLow, "Price is near Historical Low")]
    [InlineData(DealRatingReason.BuyNow_RarelyDiscounted, "Rarely Discounted")]
    [InlineData(DealRatingReason.Wait_SaleJustStarted, "Sale Just Started (Watch It)")]
    [InlineData(DealRatingReason.NotWorthIt_OverhypedNotWorthPrice, "Overhyped / Not Worth the Price")]
    public void GetReasonText_ReturnsExpectedText(int reasonId, string expected)
    {
        var result = DealRatingReason.GetReasonText(reasonId);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetReasonText_UnknownId_ReturnsUnknownReason()
    {
        var result = DealRatingReason.GetReasonText(999);
        Assert.Equal("Unknown Reason", result);
    }

    [Fact]
    public void GetReasonsForVerdict_BuyNow_ReturnsFourReasons()
    {
        var result = DealRatingReason.GetReasonsForVerdict(DealVerdict.BuyNow);
        Assert.Equal(4, result.Count);
        Assert.True(result.ContainsKey(DealRatingReason.BuyNow_PriceNearHistoricalLow));
        Assert.True(result.ContainsKey(DealRatingReason.BuyNow_StrongCommunityConfidence));
    }

    [Fact]
    public void GetReasonsForVerdict_Wait_ReturnsFourReasons()
    {
        var result = DealRatingReason.GetReasonsForVerdict(DealVerdict.Wait);
        Assert.Equal(4, result.Count);
        Assert.True(result.ContainsKey(DealRatingReason.Wait_LikelyBiggerDiscountSoon));
    }

    [Fact]
    public void GetReasonsForVerdict_NotWorthIt_ReturnsFourReasons()
    {
        var result = DealRatingReason.GetReasonsForVerdict(DealVerdict.NotWorthIt);
        Assert.Equal(4, result.Count);
        Assert.True(result.ContainsKey(DealRatingReason.NotWorthIt_DiscountTooSmall));
    }

    [Theory]
    [InlineData(DealRatingReason.BuyNow_PriceNearHistoricalLow, DealVerdict.BuyNow, true)]
    [InlineData(DealRatingReason.BuyNow_StrongCommunityConfidence, DealVerdict.BuyNow, true)]
    [InlineData(DealRatingReason.Wait_LikelyBiggerDiscountSoon, DealVerdict.Wait, true)]
    [InlineData(DealRatingReason.NotWorthIt_LowQualityPoorReviews, DealVerdict.NotWorthIt, true)]
    public void IsValidReasonForVerdict_ValidCombinations_ReturnsTrue(int reasonId, DealVerdict verdict, bool _)
    {
        Assert.True(DealRatingReason.IsValidReasonForVerdict(reasonId, verdict));
    }

    [Theory]
    [InlineData(DealRatingReason.Wait_LikelyBiggerDiscountSoon, DealVerdict.BuyNow)]
    [InlineData(DealRatingReason.BuyNow_PriceNearHistoricalLow, DealVerdict.Wait)]
    [InlineData(DealRatingReason.NotWorthIt_DiscountTooSmall, DealVerdict.BuyNow)]
    [InlineData(0, DealVerdict.BuyNow)]
    [InlineData(99, DealVerdict.NotWorthIt)]
    public void IsValidReasonForVerdict_InvalidCombinations_ReturnsFalse(int reasonId, DealVerdict verdict)
    {
        Assert.False(DealRatingReason.IsValidReasonForVerdict(reasonId, verdict));
    }
}
