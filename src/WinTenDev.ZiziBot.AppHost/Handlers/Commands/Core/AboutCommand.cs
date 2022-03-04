using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Services.Telegram.Extensions;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Core;

public class AboutCommand : CommandBase
{
    private readonly BotService _botService;
    private readonly TelegramService _telegramService;
    private readonly EnginesConfig _enginesConfig;

    public AboutCommand(
        IOptionsSnapshot<EnginesConfig> enginesConfig,
        BotService botService,
        TelegramService telegramService
    )
    {
        _enginesConfig = enginesConfig.Value;
        _botService = botService;
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var me = await _botService.GetMeAsync();
        var botVersion = _enginesConfig.Version;
        var company = _enginesConfig.Company;

        var buttonParsed = await _botService.GetButtonConfig("about");

        var stringBuilder = new StringBuilder()
            .Append("<b>").Append(company).Append(' ').Append(me.GetFullName()).AppendLine("</b>")
            .AppendLine("by @WinTenDev")
            .Append("<b>Version: </b>").AppendLine(botVersion);

        if (buttonParsed.Caption.IsNotNullOrEmpty())
            stringBuilder.AppendLine().AppendLine(buttonParsed.Caption);

        var sendText = stringBuilder.ToTrimmedString();

        await _telegramService.SendMessageTextAsync(
            new MessageResponseDto()
            {
                MessageText = sendText,
                ReplyMarkup = buttonParsed.Markup,
                ReplyToMessageId = 0,
                ScheduleDeleteAt = DateTime.UtcNow.AddMinutes(1)
            }
        );
    }
}