using GameShelf.Business.Services.Interfaces;
using GameShelf.Data.Persistance;
using GameShelf.Models.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameShelf.Business.Services.Implementations
{
    public class GameRatingService : IGameRatingService
    {
        private readonly ApplicationDbContext _dbContext;

        public GameRatingService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int?> GetUserRatingAsync(string dealId, string userId)
        {
            var rating = await _dbContext.GameRatings
                .FirstOrDefaultAsync(r => r.DealId == dealId && r.UserId == userId);
            return rating?.Rating;
        }

        public async Task<double> GetAverageRatingAsync(string dealId)
        {
            var ratings = await _dbContext.GameRatings
                .Where(r => r.DealId == dealId)
                .Select(r => r.Rating)
                .ToListAsync();

            if (ratings.Count == 0)
                return 0;

            return ratings.Average();
        }

        public async Task<int> GetRatingCountAsync(string dealId)
        {
            return await _dbContext.GameRatings
                .Where(r => r.DealId == dealId)
                .CountAsync();
        }

        public async Task SetRatingAsync(string dealId, string storeName, string userId, int rating)
        {
            if (rating < 1 || rating > 5)
                throw new ArgumentException("Rating must be between 1 and 5", nameof(rating));

            var existing = await _dbContext.GameRatings
                .FirstOrDefaultAsync(r => r.DealId == dealId && r.UserId == userId);

            if (existing != null)
            {
                existing.Rating = rating;
                existing.UpdatedAt = DateTime.UtcNow;
                _dbContext.GameRatings.Update(existing);
            }
            else
            {
                var newRating = new GameRating
                {
                    Id = Guid.NewGuid(),
                    DealId = dealId,
                    StoreName = storeName,
                    UserId = userId,
                    Rating = rating,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _dbContext.GameRatings.AddAsync(newRating);
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
