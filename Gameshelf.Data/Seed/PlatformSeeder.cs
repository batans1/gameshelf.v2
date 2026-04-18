using GameShelf.Data.Persistance;
using GameShelf.Models.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameShelf.Data.Seed
{
    public static class PlatformSeeder
    {
        private static readonly (string Name, string WebsiteUrl, string SupportUrl)[] AllPlatforms =
        {
            ("Steam", "https://store.steampowered.com", "https://help.steampowered.com"),
            ("GOG", "https://www.gog.com", "https://support.gog.com"),
            ("Epic Games", "https://store.epicgames.com", "https://www.epicgames.com/help"),
            ("Playstation store", "https://www.playstation.com/en-bg/", "https://www.playstation.com/en-bg/support/?smcid=pdc%3Aen-bg%3Aprimary%20nav%3Amsg-support%3Asupport"),
        };

        public static async Task<List<Platform>> SeedAsync(ApplicationDbContext dbContext)
        {
            var existing = await dbContext.Platforms.ToListAsync();
            var existingNames = existing.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            
            // Add default platforms only if they don't exist (do NOT remove custom platforms)
            var toAdd = new List<Platform>();
            foreach (var (name, websiteUrl, supportUrl) in AllPlatforms)
            {
                if (existingNames.Contains(name)) continue;
                toAdd.Add(new Platform
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    WebsiteUrl = websiteUrl,
                    SupportUrl = supportUrl
                });
            }
            if (toAdd.Count > 0)
            {
                await dbContext.Platforms.AddRangeAsync(toAdd);
                await dbContext.SaveChangesAsync();
            }

            var allPlatforms = await dbContext.Platforms.ToListAsync();
            foreach (var platform in allPlatforms)
            {
                if (await dbContext.PlatformImages.AnyAsync(i => i.PlatformId == platform.Id))
                    continue;

                var slug = platform.Name.ToLowerInvariant().Replace(" ", "_");
                var images = new List<PlatformImage>
                {
                    new() { Id = Guid.NewGuid(), PlatformId = platform.Id, ImagePath = $"/images/platforms/{slug}_1.jpg" }
                  
                };
                await dbContext.PlatformImages.AddRangeAsync(images);
            }
            await dbContext.SaveChangesAsync();
            return allPlatforms;
        }
    }
}
