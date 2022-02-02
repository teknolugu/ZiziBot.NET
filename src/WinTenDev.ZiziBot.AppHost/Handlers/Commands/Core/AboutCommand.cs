using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Core;

public class AboutCommand : CommandBase
{
    private readonly ButtonConfig _buttonConfig;
    private readonly BotService _botService;
    private readonly TelegramService _telegramService;
    private readonly EnginesConfig _enginesConfig;

    public AboutCommand(
        IOptionsSnapshot<EnginesConfig> enginesConfig,
        IOptionsSnapshot<ButtonConfig> buttonConfig,
        BotService botService,
        TelegramService telegramService
    )
    {
        _buttonConfig = buttonConfig.Value;
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

        var aboutButton = _buttonConfig.Items?.Find(x => x.Key == "about");

        var description = aboutButton?.Data.Descriptions?.JoinStr("\n\n");
        var warning = aboutButton?.Data.Warnings?.JoinStr("\n\n");
        var note = aboutButton?.Data.Notes?.JoinStr("\n\n");

        var buttonMarkup = InlineKeyboardMarkup.Empty();

        if (aboutButton?.Data.Buttons != null)
        {
            buttonMarkup = new InlineKeyboardMarkup
            (
                aboutButton
                    .Data
                    .Buttons
                    .Select
                    (
                        x => x
                            .Select(y => InlineKeyboardButton.WithUrl(y.Text, y.Url))
                    )
            );
        }

        var sb = new StringBuilder()
            .Append("<b>").Append(company).Append(' ').Append(me.GetFullName()).AppendLine("</b>")
            .AppendLine("by @WinTenDev")
            .Append("<b>Version: </b>").AppendLine(botVersion);

        if (description.IsNotNullOrEmpty()) sb.AppendLine().AppendLine(description);
        if (warning.IsNotNullOrEmpty()) sb.AppendLine().AppendLine(warning);
        if (!await _botService.IsProd()) sb.AppendLine().AppendLine(note);

        var sendText = sb.ToTrimmedString();

        await _telegramService.SendTextMessageAsync(sendText, buttonMarkup, 0);
    }
}