using System;
using System.Reflection;
using System.Threading.Tasks;
using SerilogTimings;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Telegram.Extensions;

public static class TelegramServiceCoreExtension
{
    public static async Task SendPingAsync(this TelegramService telegramService)
    {
        var op = Operation.Begin("Ping Command handler");
        var chatId = telegramService.ChatId;

        var keyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Ping", "PONG"));

        var htmlMessage = HtmlMessage.Empty
            .TextBr("ℹ Pong!!");

        var featureConfig = await telegramService.GetFeatureConfig("ping");

        if ((telegramService.IsPrivateChat &&
             telegramService.IsFromSudo) ||
            (featureConfig.AllowsAt?.Contains(chatId.ToString()) ?? false))
        {
            htmlMessage.Bold("📅 Date: ").Code(DateTime.UtcNow.ToDetailDateTimeString()).Br()
                .TextBr("🎛 Engine Info.").Br();

            var getWebHookInfo = await telegramService.Client.GetWebhookInfoAsync();
            if (string.IsNullOrEmpty(getWebHookInfo.Url))
                htmlMessage.Italic("Bot is running in Poll mode");
            else
                htmlMessage.Append(getWebHookInfo.ParseWebHookInfo());
        }

        await telegramService.SendTextMessageAsync(
            sendText: htmlMessage.ToString(),
            replyMarkup: keyboard,
            scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
            includeSenderMessage: true
        );

        op.Complete();
    }

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