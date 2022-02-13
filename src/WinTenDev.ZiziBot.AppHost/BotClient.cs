using Microsoft.Extensions.Options;
using Telegram.Bot.Framework;

namespace WinTenDev.ZiziBot.AppHost;

public class BotClient : BotBase
{
    public BotClient(IOptions<BotOptions<BotClient>> options) : base(options.Value)
    {
    }
}