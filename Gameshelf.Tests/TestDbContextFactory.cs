using GameShelf.Data.Persistance;
using GameShelf.Models.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GameShelf.Tests;

/// <summary>
/// Creates an in-memory ApplicationDbContext for unit tests.
/// </summary>
public static class TestDbContextFactory
{
    public static ApplicationDbContext Create(string databaseName = "TestDb")
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }

    /// <summary>
    /// Seeds a platform, a game deal, and optionally deal ratings with a user for testing DealRatingService.
    /// </summary>
    public static async Task<(Platform Platform, GameDeal GameDeal, IdentityUser User)> SeedForDealRatingsAsync(
        ApplicationDbContext db)
    {
        var user = new IdentityUser { Id = Guid.NewGuid().ToString(), UserName = "testuser@test.com", Email = "testuser@test.com" };
        var platform = new Platform
        {
            Id = Guid.NewGuid(),
            Name = "Steam",
            WebsiteUrl = "https://store.steampowered.com",
            SupportUrl = "https://help.steampowered.com"
        };
        var gameDeal = new GameDeal
        {
            Id = Guid.NewGuid(),
            PlatformId = platform.Id,
            Name = "Test Game",
            Source = DealSource.Custom,
            DealId = null,
            StoreName = "Steam",
            Price = 9.99m,
            OriginalPrice = 19.99m,
            IsActive = true,
            IsAvailable = true,
            DisplayOrder = 0
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.Platforms.Add(platform);
        db.GameDeals.Add(gameDeal);
        await db.SaveChangesAsync();

        return (platform, gameDeal, user);
    }
}
