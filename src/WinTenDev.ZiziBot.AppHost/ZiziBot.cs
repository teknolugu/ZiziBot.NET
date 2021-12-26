using Microsoft.Extensions.Options;
using Telegram.Bot.Framework;

namespace WinTenDev.ZiziBot.AppHost;

public class ZiziBot : BotBase
{
    public ZiziBot(IOptions<BotOptions<ZiziBot>> options) : base(options.Value)
    {
    }
}