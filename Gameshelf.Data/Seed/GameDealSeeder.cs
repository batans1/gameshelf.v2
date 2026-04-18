using GameShelf.Data.Persistance;
using GameShelf.Models.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameShelf.Data.Seed;

public static class GameDealSeeder
{
    public static async Task<List<GameDeal>> SeedAsync(ApplicationDbContext dbContext, List<Platform> platforms)
    {
       

       
        if (platforms == null || platforms.Count == 0)
            return await dbContext.GameDeals.ToListAsync();

        var deals = new List<GameDeal>();

        foreach (var platform in platforms)
        {
            // Only seed sample deals for PlayStation store in this seeder.
            if (platform.Name == "Playstation store")
            {
                int order = 0;
                var sampleDeals = new[] {
                    ("Cyberpunk 2077", "Open-world RPG", 29.99m, 59.99m, 50),
                    ("Spider-man 2", "Fan Favourite hero game", 39.99m, 59.99m, 33),
                   
                };

                foreach (var (name, desc, price, original, discount) in sampleDeals)
                {
                    bool exists = await dbContext.GameDeals
                    .AnyAsync(d => d.PlatformId == platform.Id && d.Name == name);

                    if (exists)
                        continue;

                    deals.Add(new GameDeal
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        Description = desc,
                        Price = price,
                        OriginalPrice = original,
                        DiscountPercent = discount,
                        IsActive = true,
                        IsAvailable = true,
                        DisplayOrder = order++,
                        ImageUrl = name switch
                        {
                            "Cyberpunk 2077" => "https://image.api.playstation.com/vulcan/ap/rnd/202202/1517/UyPJCxbE3EoeLtUxjoFBnsD4.png",
                            "Spider-man 2" => "https://gmedia.playstation.com/is/image/SIEPDC/spider-man-2-screenshot-venom-4k-legal-13jul23?$1600px$",
                            _ => "/images/deals/placeholder.jpg"
                        },
                        DealUrl = name switch { "Cyberpunk 2077" => "https://www.playstation.com/en-bg/games/cyberpunk-2077/", "Spider-man 2" => "https://www.playstation.com/en-bg/games/marvels-spider-man-2/" },
                        PlatformId = platform.Id,
                        Source = DealSource.Custom,
                        StoreName = platform.Name
                    });
                }
            }
        }

       
        if (deals.Count == 0)
            return await dbContext.GameDeals.ToListAsync();

       
        await dbContext.GameDeals.AddRangeAsync(deals);
        await dbContext.SaveChangesAsync();

        return await dbContext.GameDeals.ToListAsync();
    }
}
