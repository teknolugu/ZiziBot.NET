using Allowed.Telegram.Bot.Attributes;
using Allowed.Telegram.Bot.Controllers;
using Allowed.Telegram.Bot.Models;
using Telegram.Bot;

namespace WinTenDev.ZiziBot.Alpha4.Controllers;

public class PingController : CommandController
{
    [Command("start")]
    [Command("ping")]
    public async Task Start(MessageData data)
    {
        await data.Client.SendTextMessageAsync(data.Message.From!.Id, "You pressed: /start");
    }
}