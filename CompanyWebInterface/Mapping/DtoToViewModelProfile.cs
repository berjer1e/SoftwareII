using System;
using System.Collections.Generic;
using AutoMapper;
using CompanyDataTransferObject;
using CompanyDataTransferObject.Constants;
using CompanyWebInterface.Models;

namespace CompanyWebInterface.Mapping;

public class DtoToViewModelProfile : Profile
{
    public const string ProductNamesKey = "ProductNames";
    public const string UserNamesKey = "UserNames";

    public DtoToViewModelProfile()
    {
        CreateMap<AuctionDTO, AuctionDisplayViewModel>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom((src, _, _, ctx) =>
                ResolveName(ctx, ProductNamesKey, src.ProductId, src.ProductId.ToString().Substring(0, 8))))
            .ForMember(dest => dest.ManagerName, opt => opt.MapFrom((src, _, _, ctx) =>
                ResolveName(ctx, UserNamesKey, src.AuctionManagerId, "—")));

        CreateMap<AuctionCreateViewModel, AuctionDTO>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId ?? Guid.Empty))
            .ForMember(dest => dest.StartPrice, opt => opt.MapFrom(src => src.StartPrice ?? 0m))
            .ForMember(dest => dest.EndPrice, opt => opt.MapFrom(src => src.StartPrice ?? 0m))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true));

        CreateMap<UserDTO, UserBalanceViewModel>()
            .ForMember(dest => dest.RoleDisplayName, opt => opt.MapFrom(src => AppRoles.GetDisplayName(src.RoleType)));
    }

    private static string ResolveName(ResolutionContext ctx, string key, Guid id, string fallback)
    {
        if (ctx.Items.TryGetValue(key, out var obj) && obj is IReadOnlyDictionary<Guid, string> dict)
        {
            return dict.TryGetValue(id, out var name) ? name : fallback;
        }

        return fallback;
    }
}
