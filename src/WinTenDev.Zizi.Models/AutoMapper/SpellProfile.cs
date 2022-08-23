using AutoMapper;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Models.Entities.MongoDb.Internal;

namespace WinTenDev.Zizi.Models.AutoMapper;

public class SpellProfile : Profile
{
    public SpellProfile()
    {
        CreateMap<SpellEntity, SpellDto>().ReverseMap();
    }
}