using GameShelf.Business.Repositories.Interfaces;
using GameShelf.Business.Services.Interfaces;
using GameShelf.Data.Persistance;
using GameShelf.Models.Domain.Entities;
using GameShelf.Models.ViewModels.Platforms;
using Microsoft.EntityFrameworkCore;

namespace GameShelf.Business.Services.Implementations
{
    public class DealClickService : IDealClickService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IRepository<Platform> _platformRepository;

        public DealClickService(ApplicationDbContext dbContext, IRepository<Platform> platformRepository)
        {
            _dbContext = dbContext;
            _platformRepository = platformRepository;
        }

        public async Task LogClickAsync(string dealId, string storeName, string gameTitle, string dealUrl, string? userId, string? ipAddress)
        {
            var click = new DealClick
            {
                Id = Guid.NewGuid(),
                DealId = dealId,
                StoreName = storeName,
                GameTitle = gameTitle,
                DealUrl = dealUrl,
                UserId = userId,
                IpAddress = ipAddress,
                ClickedAt = DateTime.UtcNow
            };

            await _dbContext.DealClicks.AddAsync(click);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<DealClickViewModel>> GetClicksForPlatformAsync(string platformName, string userId)
        {
            // Verify user is owner of this platform
            var platform = await _platformRepository.Query()
                .Include(p => p.Owners)
                .FirstOrDefaultAsync(p => p.Name == platformName);

            if (platform == null || !platform.Owners.Any(o => o.UserId == userId))
                return Enumerable.Empty<DealClickViewModel>();

            var clicks = await _dbContext.DealClicks
                .Where(c => c.StoreName == platformName)
                .GroupBy(c => new { c.DealId, c.GameTitle, c.DealUrl })
                .Select(g => new DealClickViewModel
                {
                    DealId = g.Key.DealId,
                    GameTitle = g.Key.GameTitle,
                    DealUrl = g.Key.DealUrl,
                    ClickCount = g.Count(),
                    LastClickedAt = g.Max(c => c.ClickedAt)
                })
                .OrderByDescending(c => c.ClickCount)
                .ThenByDescending(c => c.LastClickedAt)
                .ToListAsync();

            return clicks;
        }
    }
}
