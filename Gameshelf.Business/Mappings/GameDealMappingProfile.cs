using AutoMapper;
using GameShelf.Models.Domain.Entities;
using GameShelf.Models.ViewModels.GameDeals;

namespace GameShelf.Business.Mappings;

public class GameDealMappingProfile : Profile
{
    public GameDealMappingProfile()
    {
        CreateMap<GameDeal, GameDealViewModel>()
            .ForMember(dest => dest.PlatformName, opt => opt.MapFrom(src => src.Platform.Name));
        CreateMap<GameDealCreateOrEditViewModel, GameDeal>()
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.PriceUsd))
            .ForMember(dest => dest.OriginalPrice, opt => opt.MapFrom(src => src.OriginalPriceUsd));
    }
}
