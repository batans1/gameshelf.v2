using GameShelf.Models.ViewModels.Platforms;

namespace GameShelf.Business.Services.Interfaces
{
    public interface IDealClickService
    {
        Task LogClickAsync(string dealId, string storeName, string gameTitle, string dealUrl, string? userId, string? ipAddress);
        Task<IEnumerable<DealClickViewModel>> GetClicksForPlatformAsync(string platformName, string userId);
    }
}
