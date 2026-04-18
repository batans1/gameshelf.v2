using GameShelf.Models.ViewModels.GameDeals;

namespace GameShelf.Business.Services.Interfaces
{
    public interface IGameDealService
    {
        Task<IEnumerable<GameDealViewModel>> GetAllAsync();
        Task<GameDealViewModel?> GetByIdAsync(Guid id);
        Task<IEnumerable<GameDealViewModel>> GetByPlatformIdAsync(Guid platformId);
        Task<IEnumerable<GameDealViewModel>> GetDealsWithDiscountAsync(int? minDiscountPercent = null);
        Task<GameDealViewModel> CreateAsync(GameDealCreateOrEditViewModel model);
        Task<GameDealViewModel> UpdateAsync(Guid id, GameDealCreateOrEditViewModel model);
        Task<GameDealViewModel> DeleteAsync(Guid id);
    }
}
