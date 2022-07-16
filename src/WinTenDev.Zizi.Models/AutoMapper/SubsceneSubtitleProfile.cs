using AutoMapper;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Models.Entities.MongoDb;

namespace WinTenDev.Zizi.Models.AutoMapper;

public class SubsceneSubtitleProfile : Profile
{
    public SubsceneSubtitleProfile()
    {
        CreateMap<SubsceneSubtitleItem, SubsceneSubtitleDto>()
            .ForMember(
                dto =>
                    dto.MovieUrl,
                opt =>
                    opt.MapFrom(item => "https://subscene.com" + item.MovieUrl)
            );
    }
}