using AutoMapper;
using WinTenDev.Zizi.Models.Entities.MongoDb.Internal;

namespace WinTenDev.Zizi.Models.AutoMapper;

public class ArticleSentProfile : Profile
{
    public ArticleSentProfile()
    {
        CreateMap<ArticleSent, ArticleSentDto>().ReverseMap();
    }
}