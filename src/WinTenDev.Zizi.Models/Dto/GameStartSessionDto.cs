using WinTenDev.Zizi.Models.Entities.MongoDb.Internal.Games;

namespace WinTenDev.Zizi.Models.Dto;

public class GameStartSessionDto
{
    public long ChatId { get; set; }
    public string GameName { get; set; }
    public SessionChatTebakKataEntity SessionChat { get; set; }
}