using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Core;

public class DebugCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public DebugCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);

        var msg = _telegramService.AnyMessage;
        var json = msg.ToJson(true);

        Log.Information("Debug: {0}", json.Length.ToString());

        var sendText = $"Debug:\n" +
                       $"<code>{json}</code>";
        await _telegramService.SendTextMessageAsync(sendText);
    }
}