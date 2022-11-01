using AutoMapper;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Models.Entities.MongoDb.Internal;

namespace WinTenDev.Zizi.Models.AutoMapper;

public class WordFilterProfile : Profile
{
    public WordFilterProfile()
    {
        CreateMap<WordFilterEntity, WordFilterDto>().ReverseMap();
    }
}