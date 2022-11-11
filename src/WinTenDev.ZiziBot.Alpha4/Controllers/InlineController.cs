using Allowed.Telegram.Bot.Attributes;
using Allowed.Telegram.Bot.Controllers;
using Allowed.Telegram.Bot.Models;
using Telegram.Bot;

namespace WinTenDev.ZiziBot.Alpha4.Controllers;

public class InlineController : CommandController
{
    [InlineQuery]
    public async Task InlineSample(InlineQueryData data)
    {
        await data.Client.SendTextMessageAsync(data.InlineQuery.From.Id, $"You enter: {data.InlineQuery.Query}");
    }
}