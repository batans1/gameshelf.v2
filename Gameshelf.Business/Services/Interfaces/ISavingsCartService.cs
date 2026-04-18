using GameShelf.Models.ViewModels.Profile;

namespace GameShelf.Business.Services.Interfaces
{
    public interface ISavingsCartService
    {
        Task<SavingsCartSummaryViewModel> GetSummaryAsync(string userId);
        Task AddAsync(string userId, Guid gameDealId);
        Task RemoveAsync(string userId, Guid gameDealId);
        Task<bool> ContainsAsync(string userId, Guid gameDealId);
    }
}
