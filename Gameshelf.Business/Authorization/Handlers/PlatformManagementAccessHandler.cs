using GameShelf.Business.Authorization.Requirements;
using GameShelf.Business.Repositories.Interfaces;
using GameShelf.Models.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace GameShelf.Business.Authorization.Handlers
{
    public class PlatformManagementAccessHandler : AuthorizationHandler<PlatformManagementAccessRequirement, Guid>
    {
        private readonly IRepository<Platform> _platformRepository;

        public PlatformManagementAccessHandler(IRepository<Platform> platformRepository)
        {
            _platformRepository = platformRepository;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PlatformManagementAccessRequirement requirement, Guid platformId)
        {
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return;
            }

            if (!context.User.IsInRole("PlatformOwner"))
            {
                return;
            }

            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return;

            var platform = await _platformRepository.GetByIdAsync(platformId, p => p.Owners);
            if (platform == null)
                return;

            if (platform.Owners.Any(o => o.UserId == userId))
                context.Succeed(requirement);
        }
    }
}
