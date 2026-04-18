using GameShelf.Data.Persistance;
using GameShelf.Models.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace GameShelf.Data.Seed
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var platforms = await PlatformSeeder.SeedAsync(dbContext);
            await GameDealSeeder.SeedAsync(dbContext, platforms);
            await UserSeeder.SeedAsync(serviceProvider, dbContext, platforms);
        }
    }
}
