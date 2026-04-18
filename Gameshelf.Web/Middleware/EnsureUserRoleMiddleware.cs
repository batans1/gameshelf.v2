using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace GameShelf.Web.Middleware;


/// Ensures every authenticated user has at least the "User" role
public class EnsureUserRoleMiddleware
{
    private const string RoleName = "User";
    private const string CacheKeyPrefix = "EnsureUserRole_";
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(24);
    private readonly RequestDelegate _next;

    public EnsureUserRoleMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IMemoryCache cache)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = userManager.GetUserId(context.User);
            if (!string.IsNullOrEmpty(userId))
            {
                var cacheKey = CacheKeyPrefix + userId;
                if (!cache.TryGetValue(cacheKey, out _))
                {
                    var user = await userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        var roles = await userManager.GetRolesAsync(user);
                        if (roles.Count == 0)
                        {
                            if (!await roleManager.RoleExistsAsync(RoleName))
                                await roleManager.CreateAsync(new IdentityRole(RoleName));
                            await userManager.AddToRoleAsync(user, RoleName);
                        }
                        cache.Set(cacheKey, true, CacheExpiry);
                    }
                }
            }
        }

        await _next(context);
    }
}

