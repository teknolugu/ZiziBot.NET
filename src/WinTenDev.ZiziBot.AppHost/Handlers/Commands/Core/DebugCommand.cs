using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

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

        var cmd = _telegramService.GetCommand(withoutSlash: true);

        var msg = _telegramService.Update;

        var debug = cmd switch
        {
            "json" => msg.ToJson(true),
            _ => msg.ToYaml().HtmlEncode()
        };

        var sendText = $"Debug:\n" +
                       $"<code>{debug}</code>";
        await _telegramService.SendTextMessageAsync(sendText);
    }
}
