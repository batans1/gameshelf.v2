using AutoMapper;
using GameShelf.Business.Repositories.Interfaces;
using GameShelf.Business.Services.Interfaces;
using GameShelf.Models.Domain.Entities;
using GameShelf.Models.ViewModels.GameDeals;
using Microsoft.EntityFrameworkCore;

namespace GameShelf.Business.Services.Implementations;

public class GameDealService : IGameDealService
{
    private readonly IRepository<GameDeal> _dealRepository;
    private readonly IRepository<Platform> _platformRepository;
    private readonly IMapper _mapper;
    private readonly IExchangeRateService _exchangeRateService;

    public GameDealService(
        IRepository<GameDeal> dealRepository,
        IRepository<Platform> platformRepository,
        IMapper mapper,
        IExchangeRateService exchangeRateService)
    {
        _dealRepository = dealRepository;
        _platformRepository = platformRepository;
        _mapper = mapper;
        _exchangeRateService = exchangeRateService;
    }

    public async Task<IEnumerable<GameDealViewModel>> GetAllAsync()
    {
        var deals = await _dealRepository.GetAllAsync(d => d.Platform);
        return _mapper.Map<IEnumerable<GameDealViewModel>>(deals);
    }

    public async Task<GameDealViewModel?> GetByIdAsync(Guid id)
    {
        var deal = await _dealRepository.Query()
            .Where(d => d.Id == id)
            .Include(d => d.Platform)
            .FirstOrDefaultAsync();
        return _mapper.Map<GameDealViewModel?>(deal);
    }

    public async Task<IEnumerable<GameDealViewModel>> GetByPlatformIdAsync(Guid platformId)
    {
        var deals = await _dealRepository.Query()
            .Where(d => d.PlatformId == platformId)
            .Include(d => d.Platform)
            .OrderBy(d => d.DisplayOrder)
            .ToListAsync();
        return _mapper.Map<IEnumerable<GameDealViewModel>>(deals);
    }

    public async Task<IEnumerable<GameDealViewModel>> GetDealsWithDiscountAsync(int? minDiscountPercent = null)
    {
        var minDiscount = minDiscountPercent ?? 0;
        var deals = await _dealRepository.Query()
            .Where(d => d.DiscountPercent != null && d.DiscountPercent > minDiscount)
            .Include(d => d.Platform)
            .OrderByDescending(d => d.DiscountPercent)
            .ToListAsync();
        return _mapper.Map<IEnumerable<GameDealViewModel>>(deals);
    }

    public async Task<GameDealViewModel> CreateAsync(GameDealCreateOrEditViewModel model)
    {
        var platform = await _platformRepository.GetByIdAsync(model.PlatformId);
        if (platform == null)
            throw new KeyNotFoundException($"Platform ID {model.PlatformId} not found.");

        var deal = _mapper.Map<GameDeal>(model);
        deal.Id = Guid.NewGuid();
        deal.Source = DealSource.Custom; // Custom deals are manually added
        deal.StoreName = platform.Name; // Store platform name for quick access
        
        // Auto-calculate discount percent if OriginalPrice > Price
        if (deal.OriginalPrice.HasValue && deal.OriginalPrice.Value > deal.Price && deal.OriginalPrice.Value > 0)
        {
            deal.DiscountPercent = (int)Math.Round((1 - (deal.Price / deal.OriginalPrice.Value)) * 100);
        }
        
        await _dealRepository.AddAsync(deal);
        await _dealRepository.CommitAsync();
        return _mapper.Map<GameDealViewModel>(deal);
    }

    public async Task<GameDealViewModel> UpdateAsync(Guid id, GameDealCreateOrEditViewModel model)
    {
        var deal = await _dealRepository.GetByIdAsync(id);
        if (deal == null)
            throw new KeyNotFoundException($"Game deal ID {id} not found.");
        var platform = await _platformRepository.GetByIdAsync(model.PlatformId);
        if (platform == null)
            throw new KeyNotFoundException($"Platform ID {model.PlatformId} not found.");

        _mapper.Map(model, deal);
        
        // Auto-calculate discount percent if OriginalPrice > Price
        if (deal.OriginalPrice.HasValue && deal.OriginalPrice.Value > deal.Price && deal.OriginalPrice.Value > 0)
        {
            deal.DiscountPercent = (int)Math.Round((1 - (deal.Price / deal.OriginalPrice.Value)) * 100);
        }
        else if (deal.OriginalPrice == null || deal.OriginalPrice <= deal.Price)
        {
            deal.DiscountPercent = null;
        }
        
        _dealRepository.Update(deal);
        await _dealRepository.CommitAsync();
        return _mapper.Map<GameDealViewModel>(deal);
    }

    public async Task<GameDealViewModel> DeleteAsync(Guid id)
    {
        var deal = await _dealRepository.GetByIdAsync(id);
        if (deal == null)
            throw new KeyNotFoundException($"Game deal ID {id} not found.");

        _dealRepository.Remove(deal);
        await _dealRepository.CommitAsync();
        return _mapper.Map<GameDealViewModel>(deal);
    }
}
