using System;
using System.Reflection;
using System.Threading.Tasks;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Telegram.Extensions;

public static class TelegramServiceCoreExtension
{
    public static async Task SendAboutAsync(this TelegramService telegramService)
    {
        var enginesConfig = telegramService.EnginesConfig;

        var me = await telegramService.BotService.GetMeAsync();
        var botVersion = enginesConfig.Version;
        var company = enginesConfig.Company;
        var description = enginesConfig.Description;

        var aboutFeature = await telegramService.GetFeatureConfig();
        var currentAssembly = Assembly.GetExecutingAssembly().GetName();
        var assemblyVersion = currentAssembly.Version?.ToString();
        var buildDate = AssemblyUtil.GetBuildDate();

        var htmlMessage = HtmlMessage.Empty
            .Bold(company).Text(" ").Bold(me.GetFullName()).Text(" ").Code(botVersion).Br()
            .Bold("Version: ").Code(assemblyVersion).Br()
            .Bold("BuildDate: ").Code(buildDate.ToDetailDateTimeString()).Br();

        if (description.IsNotNullOrEmpty())
        {
            htmlMessage.Text(enginesConfig.Description).Br();
        }

        if (aboutFeature.Caption.IsNotNullOrEmpty())
        {
            htmlMessage.Br().Text(aboutFeature.Caption);
        }

        var sendText = htmlMessage.ToString();

        await telegramService.SendTextMessageAsync(
            sendText: sendText,
            replyMarkup: aboutFeature.Markup,
            replyToMsgId: 0,
            scheduleDeleteAt: DateTime.UtcNow.AddMinutes(2),
            includeSenderMessage: true
        );
    }
}