using GameShelf.Business.Authorization.Handlers;
using GameShelf.Business.Authorization.Requirements;
using GameShelf.Business.Repositories.Interfaces;
using GameShelf.Models.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Moq;
using System.Security.Claims;
using Xunit;

namespace GameShelf.Tests;

public class PlatformManagementAccessHandlerTests
{
    private static ClaimsPrincipal CreateUser(string userId, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private static AuthorizationHandlerContext CreateContext(
        PlatformManagementAccessRequirement requirement,
        ClaimsPrincipal user,
        Guid platformId)
    {
        return new AuthorizationHandlerContext(
            requirements: new[] { requirement },
            user: user,
            resource: platformId);
    }

    [Fact]
    public async Task HandleRequirementAsync_AdminUser_SucceedsWithoutRepositoryLookup()
    {
        var requirement = new PlatformManagementAccessRequirement();
        var user = CreateUser("admin-id", "Admin");
        var platformId = Guid.NewGuid();

        var repoMock = new Mock<IRepository<Platform>>(MockBehavior.Strict);
        var handler = new PlatformManagementAccessHandler(repoMock.Object);
        var context = CreateContext(requirement, user, platformId);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        repoMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleRequirementAsync_PlatformOwnerOfPlatform_Succeeds()
    {
        var requirement = new PlatformManagementAccessRequirement();
        var userId = Guid.NewGuid().ToString();
        var user = CreateUser(userId, "PlatformOwner");
        var platformId = Guid.NewGuid();

        var platform = new Platform
        {
            Id = platformId,
            Name = "Steam",
            WebsiteUrl = "https://store.steampowered.com",
            SupportUrl = "https://help.steampowered.com",
            Owners = new List<PlatformOwner>
            {
                new() { PlatformId = platformId, UserId = userId }
            }
        };

        var repoMock = new Mock<IRepository<Platform>>();
        repoMock
            .Setup(r => r.GetByIdAsync(platformId, It.IsAny<System.Linq.Expressions.Expression<Func<Platform, object>>[]>()))
            .ReturnsAsync(platform);

        var handler = new PlatformManagementAccessHandler(repoMock.Object);
        var context = CreateContext(requirement, user, platformId);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        repoMock.Verify(r => r.GetByIdAsync(platformId, It.IsAny<System.Linq.Expressions.Expression<Func<Platform, object>>[]>()), Times.Once);
    }

    [Fact]
    public async Task HandleRequirementAsync_PlatformOwnerNotOwnerOfPlatform_Fails()
    {
        var requirement = new PlatformManagementAccessRequirement();
        var userId = Guid.NewGuid().ToString();
        var user = CreateUser(userId, "PlatformOwner");
        var platformId = Guid.NewGuid();

        var platform = new Platform
        {
            Id = platformId,
            Name = "GOG",
            WebsiteUrl = "https://www.gog.com",
            SupportUrl = "https://support.gog.com",
            Owners = new List<PlatformOwner>
            {
                new() { PlatformId = platformId, UserId = "someone-else" }
            }
        };

        var repoMock = new Mock<IRepository<Platform>>();
        repoMock
            .Setup(r => r.GetByIdAsync(platformId, It.IsAny<System.Linq.Expressions.Expression<Func<Platform, object>>[]>()))
            .ReturnsAsync(platform);

        var handler = new PlatformManagementAccessHandler(repoMock.Object);
        var context = CreateContext(requirement, user, platformId);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}

