using GameShelf.Data.Persistance;
using GameShelf.Models.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameShelf.Data.Seed
{
    internal static class UserSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider, ApplicationDbContext dbContext, List<Platform> platforms)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            
            await SeedRoles(roleManager);
            await SeedUsers(userManager);
            await SeedPlatformOwners(dbContext, userManager, platforms);
        }

        private static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            foreach (var roleName in new[] { "Admin", "PlatformOwner", "User" })
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        private static async Task SeedUsers(UserManager<IdentityUser> userManager)
        {
            await SeedUser(userManager, "admin@gameshelf.com", "Admin#123", "Admin");
            // Create separate owner accounts for each platform
            await SeedUser(userManager, "steam@gameshelf.com", "Steam#123", "PlatformOwner");
            await SeedUser(userManager, "gog@gameshelf.com", "GoodOG#123", "PlatformOwner");
            await SeedUser(userManager, "epic@gameshelf.com", "Epic#123", "PlatformOwner");
            await SeedUser(userManager, "playstation@gameshelf.com", "Playstation#123", "PlatformOwner");
            await SeedUser(userManager, "user@gameshelf.com", "User#123", "User");
        }

        private static async Task SeedUser(UserManager<IdentityUser> userManager, string email, string password, string roleName)
        {
            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                // Update password if user exists (in case password was changed)
                var token = await userManager.GeneratePasswordResetTokenAsync(existingUser);
                await userManager.ResetPasswordAsync(existingUser, token, password);
                // Ensure role is assigned
                if (!await userManager.IsInRoleAsync(existingUser, roleName))
                {
                    await userManager.AddToRoleAsync(existingUser, roleName);
                }
                return;
            }
            var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(user, roleName);
        }

        private static async Task SeedPlatformOwners(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, List<Platform> platforms)
        {
            if (!platforms.Any())
                return;

            // Map platform names to owner email addresses
            var platformOwnerMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Steam"] = "steam@gameshelf.com",
                ["GOG"] = "gog@gameshelf.com",
                ["Epic Games"] = "epic@gameshelf.com",
                ["Playstation store"] = "playstation@gameshelf.com"
            };

            foreach (var platform in platforms)
            {
                if (!platformOwnerMap.TryGetValue(platform.Name, out var ownerEmail))
                    continue;

                var owner = await userManager.FindByEmailAsync(ownerEmail);
                if (owner == null)
                    continue;

                if (await dbContext.PlatformOwners.AnyAsync(po => po.PlatformId == platform.Id && po.UserId == owner.Id))
                    continue;

                await dbContext.PlatformOwners.AddAsync(new PlatformOwner { PlatformId = platform.Id, UserId = owner.Id });
            }
            await dbContext.SaveChangesAsync();
        }
    }
}
