using GameShelf.Models.ViewModels.Platforms;

namespace GameShelf.Business.Services.Interfaces
{
    public interface IPlatformService
    {
        Task<IEnumerable<PlatformViewModel>> GetAllAsync();
        Task<PlatformViewModel?> GetByIdAsync(Guid id);
        Task<IEnumerable<PlatformViewModel>> GetByOwnerIdAsync(string ownerId);
        Task<IEnumerable<PlatformViewModel>> SearchByWebsiteAsync(string websiteUrl);
        Task<PlatformViewModel> CreateAsync(PlatformCreateOrEditViewModel model);
        Task<PlatformViewModel> UpdateAsync(Guid id, PlatformCreateOrEditViewModel model);
        Task<PlatformViewModel> DeleteAsync(Guid id);
        Task DeletePlatformLogoAsync(Guid platformId, Guid imageId);
    }
}
