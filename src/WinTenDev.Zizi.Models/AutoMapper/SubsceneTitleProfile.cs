using System;
using System.Linq;
using AutoMapper;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Models.Entities.MongoDb;

namespace WinTenDev.Zizi.Models.AutoMapper;

public class SubsceneTitleProfile : Profile
{
    public SubsceneTitleProfile()
    {
        CreateMap<SubsceneMovieSearch, SubsceneTitleDto>()
            .ForMember(
                dto =>
                    dto.MovieSlug,
                expression =>
                    expression.MapFrom(
                        src =>
                            src.MovieUrl.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault()
                    )
            )
            .ForMember(
                dto =>
                    dto.MovieUrl,
                expression =>
                    expression.MapFrom(
                        (
                            src,
                            dto
                        ) => $"https://subscene.com{src.MovieUrl}"
                    )
            );
    }
}