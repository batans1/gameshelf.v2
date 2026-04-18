using AutoMapper;
using Microsoft.AspNetCore.Identity;
using GameShelf.Models.Domain.Entities;
using GameShelf.Models.ViewModels.Platforms;

namespace GameShelf.Business.Mappings
{
    public class PlatformMappingProfile : Profile
    {
        public PlatformMappingProfile()
        {
            CreateMap<IdentityUser, PlatformOwnerDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? src.UserName ?? string.Empty));
            CreateMap<Platform, PlatformViewModel>()
                .ForMember(dest => dest.Owners, opt => opt.MapFrom(src => src.Owners.Select(x => x.User)));
            CreateMap<PlatformCreateOrEditViewModel, Platform>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Images, opt => opt.Ignore())
                .ForMember(dest => dest.Owners, opt => opt.Ignore());
            CreateMap<PlatformImage, PlatformImageViewModel>();
            CreateMap<PlatformViewModel, PlatformCreateOrEditViewModel>()
                .ForMember(dest => dest.ExistingImages, opt => opt.MapFrom(src => src.Images))
                .ForMember(dest => dest.Images, opt => opt.Ignore());
        }
    }
}
