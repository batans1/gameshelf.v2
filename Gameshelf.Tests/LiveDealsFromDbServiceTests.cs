using GameShelf.Business.Services.Implementations;
using GameShelf.Business.Services.Interfaces;
using GameShelf.Data.Persistance;
using GameShelf.Models.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace GameShelf.Tests;

public class LiveDealsFromDbServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetFeaturedDealsAsync_OrdersByBuyNowThenWaitPercent_AndMapsFields()
    {
        await using var db = CreateContext();

        var platform = new Platform
        {
            Id = Guid.NewGuid(),
            Name = "Steam",
            WebsiteUrl = "https://store.steampowered.com",
            SupportUrl = "https://help.steampowered.com"
        };
        db.Platforms.Add(platform);

        var dealHighBuyNow = new GameDeal
        {
            Id = Guid.NewGuid(),
            PlatformId = platform.Id,
            Name = "Game High",
            StoreName = "Steam",
            Source = DealSource.Custom,
            Price = 10m,
            OriginalPrice = 20m,
            IsActive = true,
            IsAvailable = true,
            DisplayOrder = 0
        };
        var dealMediumBuyNow = new GameDeal
        {
            Id = Guid.NewGuid(),
            PlatformId = platform.Id,
            Name = "Game Medium",
            StoreName = "Steam",
            Source = DealSource.Custom,
            Price = 15m,
            OriginalPrice = 30m,
            IsActive = true,
            IsAvailable = true,
            DisplayOrder = 0
        };
        var dealLowBuyNow = new GameDeal
        {
            Id = Guid.NewGuid(),
            PlatformId = platform.Id,
            Name = "Game Low",
            StoreName = "Steam",
            Source = DealSource.Custom,
            Price = 5m,
            OriginalPrice = 10m,
            IsActive = true,
            IsAvailable = true,
            DisplayOrder = 0
        };

        db.GameDeals.AddRange(dealHighBuyNow, dealMediumBuyNow, dealLowBuyNow);

        // Ratings:
        // High: 3 BuyNow
        db.DealRatings.AddRange(
            new DealRating { Id = Guid.NewGuid(), GameDealId = dealHighBuyNow.Id, UserId = "u1", Verdict = DealVerdict.BuyNow, ReasonId = 1 },
            new DealRating { Id = Guid.NewGuid(), GameDealId = dealHighBuyNow.Id, UserId = "u2", Verdict = DealVerdict.BuyNow, ReasonId = 1 },
            new DealRating { Id = Guid.NewGuid(), GameDealId = dealHighBuyNow.Id, UserId = "u3", Verdict = DealVerdict.BuyNow, ReasonId = 1 }
        );

        // Medium: 1 BuyNow, 1 Wait
        db.DealRatings.AddRange(
            new DealRating { Id = Guid.NewGuid(), GameDealId = dealMediumBuyNow.Id, UserId = "u4", Verdict = DealVerdict.BuyNow, ReasonId = 1 },
            new DealRating { Id = Guid.NewGuid(), GameDealId = dealMediumBuyNow.Id, UserId = "u5", Verdict = DealVerdict.Wait, ReasonId = 5 }
        );

        // Low: 0 BuyNow, 2 Wait
        db.DealRatings.AddRange(
            new DealRating { Id = Guid.NewGuid(), GameDealId = dealLowBuyNow.Id, UserId = "u6", Verdict = DealVerdict.Wait, ReasonId = 5 },
            new DealRating { Id = Guid.NewGuid(), GameDealId = dealLowBuyNow.Id, UserId = "u7", Verdict = DealVerdict.Wait, ReasonId = 5 }
        );

        await db.SaveChangesAsync();

        var rateServiceMock = new Mock<IExchangeRateService>();
        rateServiceMock
            .Setup(s => s.GetUsdToEurRateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1.0m); // simplify: EUR = USD

        var service = new LiveDealsFromDbService(db, rateServiceMock.Object);

        var result = (await service.GetFeaturedDealsAsync(pageNumber: 1, pageSize: 10)).ToList();

        Assert.Equal(3, result.Count);

        // Order: High (100% BuyNow), Medium (~50% BuyNow), Low (0% BuyNow)
        Assert.Equal("Game High", result[0].Title);
        Assert.Equal("Game Medium", result[1].Title);
        Assert.Equal("Game Low", result[2].Title);

        var high = result[0];
        Assert.Equal(3, high.RatingCount);
        Assert.Equal(100.0, high.BuyNowPercent);
        Assert.Equal(0.0, high.WaitPercent);
        Assert.Equal(0.0, high.NotWorthItPercent);

        // Check EUR mapping uses exchange rate (1.0 here)
        Assert.Equal(dealHighBuyNow.Price, high.SalePriceEur);
        Assert.Equal(dealHighBuyNow.OriginalPrice ?? dealHighBuyNow.Price, high.NormalPriceEur);
    }
}

