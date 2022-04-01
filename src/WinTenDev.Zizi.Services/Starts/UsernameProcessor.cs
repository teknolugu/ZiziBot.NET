using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Dto;

namespace WinTenDev.Zizi.Services.Starts
{
    public class UsernameProcessor
    {
        public MessageResponseDto Execute(string payload)
        {
            var response = new MessageResponseDto();

            var replyMarkup = new InlineKeyboardMarkup
            (
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithUrl("Pasang Username", "https://t.me/WinTenDev/29")
                    }
                }
            );

            var send = "Untuk cara pasang Username, silakan klik tombol di bawah ini";

            response.MessageText = send;
            response.ReplyMarkup = replyMarkup;

            return response;
        }
    }
}
