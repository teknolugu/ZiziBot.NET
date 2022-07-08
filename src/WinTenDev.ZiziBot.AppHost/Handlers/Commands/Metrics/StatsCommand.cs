using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Metrics;

public class StatsCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly MetricService _metricService;
    private readonly BotService _botService;

    public StatsCommand(
        TelegramService telegramService,
        MetricService metricService,
        BotService botService
    )
    {
        _telegramService = telegramService;
        _metricService = metricService;
        _botService = botService;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);

        if (!await _botService.IsBeta()) return;

        var chatId = _telegramService.Message.Chat.Id;

        // await _metricService.GetStat();
    }
}
