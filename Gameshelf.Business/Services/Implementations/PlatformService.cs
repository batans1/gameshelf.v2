using AutoMapper;
using GameShelf.Business.Repositories.Interfaces;
using GameShelf.Business.Services.Interfaces;
using GameShelf.Models.Domain.Entities;
using GameShelf.Models.ViewModels.Platforms;
using Microsoft.EntityFrameworkCore;

namespace GameShelf.Business.Services.Implementations
{
    public class PlatformService : IPlatformService
    {
        private readonly IRepository<Platform> _platformRepository;
        private readonly IRepository<PlatformImage> _platformImagesRepository;
        private readonly IImageService _imageService;
        private readonly IMapper _mapper;

        public PlatformService(IRepository<Platform> platformRepository, IRepository<PlatformImage> platformImagesRepository, IImageService imageService, IMapper mapper)
        {
            _platformRepository = platformRepository;
            _platformImagesRepository = platformImagesRepository;
            _imageService = imageService;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PlatformViewModel>> GetAllAsync()
        {
            var platforms = await _platformRepository.Query()
                .AsSplitQuery()
                .Include(p => p.Images)
                .Include(p => p.Owners)
                .ToListAsync();
            return _mapper.Map<IEnumerable<PlatformViewModel>>(platforms);
        }

        public async Task<IEnumerable<PlatformViewModel>> GetByOwnerIdAsync(string ownerId)
        {
            var platforms = await _platformRepository.Query()
                .AsSplitQuery()
                .Where(p => p.Owners.Any(o => o.UserId == ownerId))
                .Include(p => p.Images)
                .Include(p => p.Owners).ThenInclude(o => o.User)
                .ToListAsync();
            return _mapper.Map<IEnumerable<PlatformViewModel>>(platforms);
        }

        public async Task<PlatformViewModel?> GetByIdAsync(Guid id)
        {
            var platform = await _platformRepository.Query()
                .AsSplitQuery()
                .Where(p => p.Id == id)
                .Include(p => p.Images)
                .Include(p => p.Owners).ThenInclude(o => o.User)
                .FirstOrDefaultAsync();
            return _mapper.Map<PlatformViewModel?>(platform);
        }

        public async Task<IEnumerable<PlatformViewModel>> SearchByWebsiteAsync(string websiteUrl)
        {
            var platforms = await _platformRepository.GetAllAsync(p => p.Images);
            var filtered = platforms.Where(p => p.WebsiteUrl.Contains(websiteUrl, StringComparison.OrdinalIgnoreCase));
            return _mapper.Map<IEnumerable<PlatformViewModel>>(filtered);
        }

        public async Task<PlatformViewModel> CreateAsync(PlatformCreateOrEditViewModel model)
        {
            var platform = _mapper.Map<Platform>(model);
            platform.Id = Guid.NewGuid();

            // Only single logo upload
            if (model.Images != null && model.Images.Count > 0)
            {
                var file = model.Images[0];
                    var imagePath = await _imageService.UploadImageAsync(file, "platforms");
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        platform.Images.Add(new PlatformImage
                        {
                            Id = Guid.NewGuid(),
                            ImagePath = imagePath,
                            PlatformId = platform.Id,
                        });
                }
            }

            if (model.SelectedOwnerIds != null)
            {
                foreach (var userId in model.SelectedOwnerIds)
                    platform.Owners.Add(new PlatformOwner { UserId = userId });
            }

            await _platformRepository.AddAsync(platform);
            await _platformRepository.CommitAsync();
            return _mapper.Map<PlatformViewModel>(platform);
        }

        public async Task<PlatformViewModel> UpdateAsync(Guid id, PlatformCreateOrEditViewModel model)
        {
            var platform = await _platformRepository.GetByIdAsync(id, p => p.Images, p => p.Owners);
            if (platform == null)
                throw new KeyNotFoundException($"ID {id} not found.");

            _mapper.Map(model, platform);

            //  replace existing logo if one exists
            if (model.Images != null && model.Images.Count > 0)
            {
                var existingLogo = platform.Images?.FirstOrDefault();
                if (existingLogo != null)
                {
                    _imageService.DeleteImage(existingLogo.ImagePath);
                    _platformImagesRepository.Remove(existingLogo);
                }

                var file = model.Images[0];
                var imagePath = await _imageService.UploadImageAsync(file, "platforms");
                if (!string.IsNullOrEmpty(imagePath))
                {
                    await _platformImagesRepository.AddAsync(new PlatformImage
                    {
                        Id = Guid.NewGuid(),
                        ImagePath = imagePath,
                        PlatformId = platform.Id,
                    });
                }
            }

            var ownersToRemove = platform.Owners.Where(o => !model.SelectedOwnerIds.Contains(o.UserId)).ToList();
            foreach (var owner in ownersToRemove)
                platform.Owners.Remove(owner);
            foreach (var userId in model.SelectedOwnerIds)
            {
                if (!platform.Owners.Any(o => o.UserId == userId))
                    platform.Owners.Add(new PlatformOwner { PlatformId = platform.Id, UserId = userId });
            }

            _platformRepository.Update(platform);
            await _platformRepository.CommitAsync();
            return _mapper.Map<PlatformViewModel>(platform);
        }

        public async Task<PlatformViewModel> DeleteAsync(Guid id)
        {
            var platform = await _platformRepository.GetByIdAsync(id, p => p.Images);
            if (platform == null)
                throw new KeyNotFoundException($"ID {id} not found.");
            if (platform.Images != null)
            {
                foreach (var img in platform.Images)
                    _imageService.DeleteImage(img.ImagePath);
            }
            _platformRepository.Remove(platform);
            await _platformRepository.CommitAsync();
            return _mapper.Map<PlatformViewModel>(platform);
        }

        public async Task DeletePlatformLogoAsync(Guid platformId, Guid imageId)
        {
            var platform = await _platformRepository.GetByIdAsync(platformId, p => p.Images);
            if (platform == null)
                throw new KeyNotFoundException($"Platform ID {platformId} not found.");

            var image = platform.Images?.FirstOrDefault(i => i.Id == imageId);
            if (image == null)
                throw new KeyNotFoundException($"Image ID {imageId} not found.");

            _imageService.DeleteImage(image.ImagePath);
            _platformImagesRepository.Remove(image);
            await _platformRepository.CommitAsync();
        }
    }
}
