using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Framework;

namespace WinTenDev.ZiziMirror.AppHost
{
    public class ZiziMirror : BotBase
    {
        public ZiziMirror(IOptions<BotOptions<ZiziMirror>> options) : base(options.Value)
        {
        }

        public ZiziMirror(string username, ITelegramBotClient client) : base(username, client)
        {
        }

        public ZiziMirror(string username, string token) : base(username, token)
        {
        }
    }
}