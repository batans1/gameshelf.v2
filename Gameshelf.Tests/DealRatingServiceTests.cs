using GameShelf.Business.Services.Implementations;
using GameShelf.Business.Services.Interfaces;
using GameShelf.Business.Services.Moderation;
using GameShelf.Data.Persistance;
using GameShelf.Models.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameShelf.Tests;

public class DealRatingServiceTests
{
    private static ApplicationDbContext CreateContext(string dbName = null!)
    {
        var name = dbName ?? Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static DealRatingService CreateService(
        ApplicationDbContext db,
        ILiveDealSyncService? syncService = null)
    {
        var mockSync = syncService ?? new Mock<ILiveDealSyncService>().Object;
        var mockModeration = new Mock<IReviewModerationService>();
        mockModeration
            .Setup(m => m.ModerateReviewAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string userId, string reviewText, CancellationToken ct) => new ModerationOutcome
            {
                ContainsProfanity = false,
                SanitizedText = reviewText
            });
        return new DealRatingService(db, mockSync, mockModeration.Object);
    }

    [Fact]
    public async Task GetCommunityVerdictAsync_NoGameDeal_ReturnsEmpty()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var result = await service.GetCommunityVerdictAsync("nonexistent", null);

        Assert.NotNull(result);
        Assert.Equal(0, result.TotalRatings);
    }

    [Fact]
    public async Task GetCommunityVerdictAsync_CustomDealId_WithRatings_ReturnsPercentages()
    {
        await using var db = CreateContext();
        var platform = new Platform { Id = Guid.NewGuid(), Name = "Steam", WebsiteUrl = "https://steam.com", SupportUrl = "" };
        var gameDeal = new GameDeal
        {
            Id = Guid.NewGuid(),
            PlatformId = platform.Id,
            Name = "Game",
            Source = DealSource.Custom,
            StoreName = "Steam",
            Price = 10m,
            IsActive = true,
            IsAvailable = true,
            DisplayOrder = 0
        };
        var user = new IdentityUser { Id = Guid.NewGuid().ToString(), UserName = "u1", Email = "u1@t.com" };
        db.Platforms.Add(platform);
        db.GameDeals.Add(gameDeal);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.DealRatings.AddRange(
            new DealRating { Id = Guid.NewGuid(), GameDealId = gameDeal.Id, UserId = user.Id, Verdict = DealVerdict.BuyNow, ReasonId = DealRatingReason.BuyNow_PriceNearHistoricalLow },
            new DealRating { Id = Guid.NewGuid(), GameDealId = gameDeal.Id, UserId = user.Id + "x", Verdict = DealVerdict.Wait, ReasonId = DealRatingReason.Wait_LikelyBiggerDiscountSoon }
        );
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetCommunityVerdictAsync(null, gameDeal.Id);

        Assert.Equal(2, result.TotalRatings);
        Assert.Equal(1, result.BuyNow.Count);
        Assert.Equal(1, result.Wait.Count);
        Assert.Equal(0, result.NotWorthIt.Count);
        Assert.Equal(50.0, result.BuyNow.Percentage);
        Assert.Equal(50.0, result.Wait.Percentage);
    }

    [Fact]
    public async Task SetRatingAsync_InvalidReasonForVerdict_ThrowsArgumentException()
    {
        await using var db = CreateContext();
        var (_, gameDeal, user) = await TestDbContextFactory.SeedForDealRatingsAsync(db);
        var service = CreateService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SetRatingAsync(null, gameDeal.Id, "Steam", user.Id, DealVerdict.BuyNow, DealRatingReason.Wait_LikelyBiggerDiscountSoon, null));
    }

    [Fact]
    public async Task SetRatingAsync_ValidCustomDeal_AddsRating()
    {
        await using var db = CreateContext();
        var (_, gameDeal, user) = await TestDbContextFactory.SeedForDealRatingsAsync(db);
        var service = CreateService(db);

        await service.SetRatingAsync(null, gameDeal.Id, "Steam", user.Id, DealVerdict.BuyNow, DealRatingReason.BuyNow_PriceNearHistoricalLow, "Great deal!");

        var rating = await db.DealRatings.FirstOrDefaultAsync(r => r.GameDealId == gameDeal.Id && r.UserId == user.Id);
        Assert.NotNull(rating);
        Assert.Equal(DealVerdict.BuyNow, rating.Verdict);
        Assert.Equal(DealRatingReason.BuyNow_PriceNearHistoricalLow, rating.ReasonId);
        Assert.Equal("Great deal!", rating.ReviewText);
    }

    [Fact]
    public async Task SetRatingAsync_ExistingRating_UpdatesIt()
    {
        await using var db = CreateContext();
        var (_, gameDeal, user) = await TestDbContextFactory.SeedForDealRatingsAsync(db);
        db.DealRatings.Add(new DealRating
        {
            Id = Guid.NewGuid(),
            GameDealId = gameDeal.Id,
            UserId = user.Id,
            Verdict = DealVerdict.Wait,
            ReasonId = DealRatingReason.Wait_LikelyBiggerDiscountSoon
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.SetRatingAsync(null, gameDeal.Id, "Steam", user.Id, DealVerdict.NotWorthIt, DealRatingReason.NotWorthIt_DiscountTooSmall, null);

        var count = await db.DealRatings.CountAsync(r => r.GameDealId == gameDeal.Id && r.UserId == user.Id);
        Assert.Equal(1, count);
        var rating = await db.DealRatings.FirstAsync(r => r.GameDealId == gameDeal.Id && r.UserId == user.Id);
        Assert.Equal(DealVerdict.NotWorthIt, rating.Verdict);
        Assert.Equal(DealRatingReason.NotWorthIt_DiscountTooSmall, rating.ReasonId);
    }

    [Fact]
    public async Task GetRatingCountAsync_CustomDeal_ReturnsCount()
    {
        await using var db = CreateContext();
        var (_, gameDeal, user) = await TestDbContextFactory.SeedForDealRatingsAsync(db);
        db.DealRatings.Add(new DealRating
        {
            Id = Guid.NewGuid(),
            GameDealId = gameDeal.Id,
            UserId = user.Id,
            Verdict = DealVerdict.BuyNow,
            ReasonId = DealRatingReason.BuyNow_PriceNearHistoricalLow
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var count = await service.GetRatingCountAsync(null, gameDeal.Id);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetRatingCountAsync_NoDeal_ReturnsZero()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var count = await service.GetRatingCountAsync("nonexistent", null);
        Assert.Equal(0, count);
    }
}
