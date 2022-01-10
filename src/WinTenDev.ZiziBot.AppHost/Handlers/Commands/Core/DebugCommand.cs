using System.Linq;
using System.Threading.Tasks;
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

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var msg = _telegramService.AnyMessage;
        var param1 = _telegramService.MessageTextParts.ElementAtOrDefault(1);

        var debug = param1 switch
        {
            "json" => msg.ToJson(true),
            _ => msg.ToYaml().HtmlDecode()
        };

        var sendText = $"Debug:\n" +
                       $"<code>{debug}</code>";
        await _telegramService.SendTextMessageAsync(sendText);
    }
}