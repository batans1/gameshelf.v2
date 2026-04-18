using GameShelf.Business.Services.Interfaces;
using GameShelf.Data.Persistance;
using GameShelf.Models.Domain.Entities;
using GameShelf.Models.ViewModels.LiveDeals;
using Microsoft.EntityFrameworkCore;
using DealVerdict = GameShelf.Models.Domain.Entities.DealVerdict;

namespace GameShelf.Business.Services.Implementations
{
   
    /// Provides live deals from the database (synced from API on startup and every hour) for fast load times.
   
    public class LiveDealsFromDbService : IExternalDealsService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IExchangeRateService _exchangeRateService;

        public LiveDealsFromDbService(ApplicationDbContext dbContext, IExchangeRateService exchangeRateService)
        {
            _dbContext = dbContext;
            _exchangeRateService = exchangeRateService;
        }

        public async Task<IEnumerable<LiveDealDto>> GetLiveDealsAsync(string platformName, int pageNumber = 1, int pageSize = 20)
        {
            var platform = await _dbContext.Platforms
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name == platformName);
            if (platform == null)
                return Array.Empty<LiveDealDto>();

            var exchangeRate = await _exchangeRateService.GetUsdToEurRateAsync();
            var deals = await _dbContext.GameDeals
                .AsNoTracking()
                .Where(d => d.PlatformId == platform.Id && d.Source == DealSource.Live && d.IsActive && d.IsAvailable)
                .OrderByDescending(d => d.DiscountPercent)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new LiveDealDto
                {
                    Title = d.Name,
                    SalePriceUsd = d.Price,
                    NormalPriceUsd = d.OriginalPrice ?? d.Price,
                    SalePriceEur = Math.Round(d.Price * exchangeRate, 2),
                    NormalPriceEur = Math.Round((d.OriginalPrice ?? d.Price) * exchangeRate, 2),
                    SavingsPercent = d.DiscountPercent ?? 0,
                    ThumbUrl = d.ImageUrl,
                    DealUrl = d.DealUrl ?? "",
                    StoreName = d.StoreName,
                    DealId = d.DealId,
                    CustomDealId = d.Source == DealSource.Custom ? d.Id : null,
                    RatingCount = 0
                })
                .ToListAsync();

            return deals;
        }

        public async Task<IEnumerable<LiveDealDto>> GetLiveDealsAllPlatformsAsync(int pageNumber = 1, int pageSize = 20)
        {
            var exchangeRate = await _exchangeRateService.GetUsdToEurRateAsync();
            var deals = await _dbContext.GameDeals
                .AsNoTracking()
                .Where(d => d.Source == DealSource.Live && d.IsActive && d.IsAvailable)
                .OrderByDescending(d => d.DiscountPercent)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new LiveDealDto
                {
                    Title = d.Name,
                    SalePriceUsd = d.Price,
                    NormalPriceUsd = d.OriginalPrice ?? d.Price,
                    SalePriceEur = Math.Round(d.Price * exchangeRate, 2),
                    NormalPriceEur = Math.Round((d.OriginalPrice ?? d.Price) * exchangeRate, 2),
                    SavingsPercent = d.DiscountPercent ?? 0,
                    ThumbUrl = d.ImageUrl,
                    DealUrl = d.DealUrl ?? "",
                    StoreName = d.StoreName,
                    DealId = d.DealId,
                    CustomDealId = d.Source == DealSource.Custom ? d.Id : null,
                    RatingCount = 0
                })
                .ToListAsync();

            return deals;
        }

        public int GetTotalDealsCount(string platformName)
        {
            var platform = _dbContext.Platforms.AsNoTracking().FirstOrDefault(p => p.Name == platformName);
            if (platform == null) return 0;
            return _dbContext.GameDeals
                .Count(d => d.PlatformId == platform.Id && d.Source == DealSource.Live && d.IsActive && d.IsAvailable);
        }

        public int GetTotalAllDealsCount()
        {
            return _dbContext.GameDeals
                .Count(d => d.Source == DealSource.Live && d.IsActive && d.IsAvailable);
        }

        public async Task<IEnumerable<LiveDealDto>> GetFeaturedDealsAsync(int pageNumber = 1, int pageSize = 40)
        {
            var exchangeRate = await _exchangeRateService.GetUsdToEurRateAsync();
            var gameDealsWithRatings = await _dbContext.GameDeals
                .AsNoTracking()
                .Include(d => d.Ratings)
                .Where(d => d.Ratings.Any())
                .ToListAsync();

            var dealStats = gameDealsWithRatings.Select(d =>
            {
                var ratings = d.Ratings.ToList();
                var total = ratings.Count;
                var buyNowCount = ratings.Count(r => r.Verdict == DealVerdict.BuyNow);
                var waitCount = ratings.Count(r => r.Verdict == DealVerdict.Wait);
                var notWorthItCount = ratings.Count(r => r.Verdict == DealVerdict.NotWorthIt);
                return new
                {
                    GameDeal = d,
                    BuyNowPercent = (double)buyNowCount / total * 100,
                    WaitPercent = (double)waitCount / total * 100,
                    NotWorthItPercent = (double)notWorthItCount / total * 100
                };
            }).ToList();

            var sorted = dealStats
                .OrderByDescending(s => s.BuyNowPercent)
                .ThenByDescending(s => s.WaitPercent)
                .ThenByDescending(s => s.NotWorthItPercent)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(s =>
                {
                    var d = s.GameDeal;
                    return new LiveDealDto
                    {
                        Title = d.Name,
                        SalePriceUsd = d.Price,
                        NormalPriceUsd = d.OriginalPrice ?? d.Price,
                        SalePriceEur = Math.Round(d.Price * exchangeRate, 2),
                        NormalPriceEur = Math.Round((d.OriginalPrice ?? d.Price) * exchangeRate, 2),
                        SavingsPercent = d.DiscountPercent ?? 0,
                        ThumbUrl = d.ImageUrl,
                        DealUrl = d.DealUrl ?? "",
                        StoreName = d.StoreName,
                        DealId = d.DealId,
                        CustomDealId = d.Source == DealSource.Custom ? d.Id : null,
                        RatingCount = d.Ratings.Count,
                        BuyNowPercent = s.BuyNowPercent,
                        WaitPercent = s.WaitPercent,
                        NotWorthItPercent = s.NotWorthItPercent
                    };
                })
                .ToList();

            return sorted;
        }
    }
}
