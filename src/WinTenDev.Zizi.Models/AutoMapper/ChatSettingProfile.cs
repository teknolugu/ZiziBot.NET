using AutoMapper;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Models.Entities.MongoDb.Internal;

namespace WinTenDev.Zizi.Models.AutoMapper;

public class ChatSettingProfile : Profile
{
    public ChatSettingProfile()
    {
        CreateMap<ChatSettingEntity, ChatSettingDto>().ReverseMap();
    }
}