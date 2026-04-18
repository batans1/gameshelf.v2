using GameShelf.Business.Services.Interfaces;
using GameShelf.Data.Persistance;
using GameShelf.Models.Domain.Entities;
using GameShelf.Models.ViewModels.Profile;
using Microsoft.EntityFrameworkCore;

namespace GameShelf.Business.Services.Implementations
{
    public class SavingsCartService : ISavingsCartService
    {
        private readonly ApplicationDbContext _dbContext;

        public SavingsCartService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<SavingsCartSummaryViewModel> GetSummaryAsync(string userId)
        {
            var items = await _dbContext.SavingsCartItems
                .Where(i => i.UserId == userId)
                .Include(i => i.GameDeal)
                .ThenInclude(d => d.Platform)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return new SavingsCartSummaryViewModel
            {
                Items = items.Select(i => new SavingsCartItemViewModel
                {
                    GameDealId = i.GameDealId,
                    GameName = i.GameDeal.Name,
                    PlatformName = i.GameDeal.Platform.Name,
                    DealId = i.GameDeal.DealId,
                    CustomDealId = i.GameDeal.Source == DealSource.Custom ? i.GameDeal.Id : null,
                    StoreName = i.GameDeal.StoreName,
                    DealPrice = i.GameDeal.Price,
                    OriginalPrice = i.GameDeal.OriginalPrice ?? i.GameDeal.Price
                }).ToList()
            };
        }

        public async Task AddAsync(string userId, Guid gameDealId)
        {
            var exists = await _dbContext.SavingsCartItems.AnyAsync(i => i.UserId == userId && i.GameDealId == gameDealId);
            if (exists) return;

            await _dbContext.SavingsCartItems.AddAsync(new SavingsCartItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                GameDealId = gameDealId,
                CreatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveAsync(string userId, Guid gameDealId)
        {
            var item = await _dbContext.SavingsCartItems
                .FirstOrDefaultAsync(i => i.UserId == userId && i.GameDealId == gameDealId);
            if (item == null) return;

            _dbContext.SavingsCartItems.Remove(item);
            await _dbContext.SaveChangesAsync();
        }

        public Task<bool> ContainsAsync(string userId, Guid gameDealId)
            => _dbContext.SavingsCartItems.AnyAsync(i => i.UserId == userId && i.GameDealId == gameDealId);
    }
}
