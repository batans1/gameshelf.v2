using GameShelf.Business.Repositories.Implementations;
using GameShelf.Business.Services.Implementations;
using GameShelf.Data.Persistance;
using GameShelf.Models.Domain.Entities;
using GameShelf.Models.ViewModels.Platforms;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameShelf.Tests;

public class DealClickServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()    
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task LogClickAsync_PersistsClickWithAllFields()
    {
        await using var db = CreateContext();
        var platformRepo = new Repository<Platform>(db);
        var service = new DealClickService(db, platformRepo);

        var userId = Guid.NewGuid().ToString();
        var ip = "127.0.0.1";

        await service.LogClickAsync(
            dealId: "DEAL123",
            storeName: "Steam",
            gameTitle: "Test Game",
            dealUrl: "https://example.com/deal",
            userId: userId,
            ipAddress: ip);

        var click = await db.DealClicks.SingleAsync();
        Assert.Equal("DEAL123", click.DealId);
        Assert.Equal("Steam", click.StoreName);
        Assert.Equal("Test Game", click.GameTitle);
        Assert.Equal("https://example.com/deal", click.DealUrl);
        Assert.Equal(userId, click.UserId);
        Assert.Equal(ip, click.IpAddress);
        Assert.True(click.ClickedAt <= DateTime.UtcNow && click.ClickedAt > DateTime.UtcNow.AddMinutes(-5));
    }

    [Fact]
    public async Task GetClicksForPlatformAsync_UserIsOwner_ReturnsAggregatedClicks()
    {
        await using var db = CreateContext();

        var ownerId = Guid.NewGuid().ToString();
        var platform = new Platform
        {
            Id = Guid.NewGuid(),
            Name = "Steam",
            WebsiteUrl = "https://store.steampowered.com",
            SupportUrl = "https://help.steampowered.com",
            Owners = new List<PlatformOwner>
            {
                new() { PlatformId = Guid.NewGuid(), UserId = ownerId }
            }
        };

        // Ensure PlatformOwner.PlatformId matches platform.Id for the relationship
        platform.Owners.First().PlatformId = platform.Id;

        db.Platforms.Add(platform);
        db.DealClicks.AddRange(
            new DealClick { Id = Guid.NewGuid(), DealId = "D1", StoreName = "Steam", GameTitle = "Game A", DealUrl = "url1", ClickedAt = DateTime.UtcNow.AddMinutes(-10) },
            new DealClick { Id = Guid.NewGuid(), DealId = "D1", StoreName = "Steam", GameTitle = "Game A", DealUrl = "url1", ClickedAt = DateTime.UtcNow.AddMinutes(-5) },
            new DealClick { Id = Guid.NewGuid(), DealId = "D2", StoreName = "Steam", GameTitle = "Game B", DealUrl = "url2", ClickedAt = DateTime.UtcNow.AddMinutes(-1) }
        );
        await db.SaveChangesAsync();

        var platformRepo = new Repository<Platform>(db);
        var service = new DealClickService(db, platformRepo);

        var result = (await service.GetClicksForPlatformAsync("Steam", ownerId)).ToList();

        Assert.Equal(2, result.Count);

        var deal1 = result.Single(r => r.DealId == "D1");
        Assert.Equal("Game A", deal1.GameTitle);
        Assert.Equal("url1", deal1.DealUrl);
        Assert.Equal(2, deal1.ClickCount);

        var deal2 = result.Single(r => r.DealId == "D2");
        Assert.Equal(1, deal2.ClickCount);
    }

    [Fact]
    public async Task GetClicksForPlatformAsync_UserNotOwner_ReturnsEmpty()
    {
        await using var db = CreateContext();

        var platform = new Platform
        {
            Id = Guid.NewGuid(),
            Name = "Steam",
            WebsiteUrl = "https://store.steampowered.com",
            SupportUrl = "https://help.steampowered.com",
            Owners = new List<PlatformOwner>()
        };
        db.Platforms.Add(platform);
        db.DealClicks.Add(new DealClick
        {
            Id = Guid.NewGuid(),
            DealId = "D1",
            StoreName = "Steam",
            GameTitle = "Game A",
            DealUrl = "url1",
            ClickedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var platformRepo = new Repository<Platform>(db);
        var service = new DealClickService(db, platformRepo);

        var result = await service.GetClicksForPlatformAsync("Steam", "some-other-user");

        Assert.Empty(result);
    }
}

