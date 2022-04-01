using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SerilogTimings;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Starts;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Telegram.Extensions;

public static class TelegramServiceCoreExtension
{
    public static TService GetRequiredService<TService>(this TelegramService telegramService)
    {
        var serviceProvider = telegramService.ServiceProvider;
        var resolvedService = serviceProvider.GetRequiredService<TService>();

        return resolvedService;
    }

    public static async Task SendStartAsync(this TelegramService telegramService)
    {
        var enginesConfig = telegramService.EnginesConfig;
        var rulesProcessor = telegramService.GetRequiredService<RulesProcessor>();
        var usernameProcessor = telegramService.GetRequiredService<UsernameProcessor>();

        var msg = telegramService.Message;
        var partText = msg.Text.SplitText(" ").ToArray();
        var startArg = partText.ElementAtOrDefault(1);
        var startArgs = startArg?.Split("_");
        var startCmd = startArgs?.FirstOrDefault();

        Log.Debug("Start Args: {StartArgs}", startArgs);

        var getMe = await telegramService.BotService.GetMeAsync();
        var aboutHeader = getMe.GetAboutHeader();
        var urlStart = await telegramService.BotService.GetUrlStart("start=help");
        var urlAddTo = await telegramService.BotService.GetUrlStart("startgroup=new");

        var botName = getMe.GetFullName();
        var botVer = enginesConfig.Version;
        var botCompany = enginesConfig.Company;

        var winTenDev = botCompany.MkUrl("https://t.me/WinTenDev");
        var ziziDocs = "https://docs.zizibot.winten.my.id";
        var levelStandardUrl = $"{ziziDocs}/glosarium/admin-dengan-level-standard";
        var levelStandard = @"Level standard".MkUrl(levelStandardUrl);

        var sendText = $"🤖 {aboutHeader}" +
                       $"\nby {winTenDev}." +
                       $"\n\nAdalah bot debugging dan manajemen grup yang di lengkapi dengan alat keamanan. " +
                       $"Agar saya bekerja dengan fitur penuh di sebuah Grup, jadikan saya admin dengan {levelStandard}. " +
                       $"\n\nSaran dan fitur bisa di ajukan di @WinTenDevSupport atau @TgBotID.";

        var result = startCmd switch
        {
            "rules" => await rulesProcessor.Execute(startArgs.ElementAtOrDefault(1)),
            "set-username" => usernameProcessor.Execute(startArgs.ElementAtOrDefault(1)),
            _ => null
        };

        if (result != null)
        {
            await telegramService.SendTextMessageAsync(
                sendText: result.MessageText,
                replyMarkup: result.ReplyMarkup,
                disableWebPreview: result.DisableWebPreview
            );

            return;
        }

        var keyboard = new InlineKeyboardMarkup
        (
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithUrl("Bantuan", ziziDocs),
                    InlineKeyboardButton.WithUrl("Pasang Username", "https://t.me/WinTenDev/29")
                },
                new[]
                {
                    InlineKeyboardButton.WithUrl("Tambahkan ke Grup", urlAddTo)
                }
            }
        );

        await telegramService.SendTextMessageAsync(
            sendText: sendText,
            replyMarkup: keyboard,
            disableWebPreview: true
        );
    }

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

        var htmlMessage = HtmlMessage.Empty;
        var chatId = telegramService.ChatId;
        var me = await telegramService.BotService.GetMeAsync();
        var aboutFeature = await telegramService.GetFeatureConfig();
        var description = enginesConfig.Description;

        htmlMessage.Append(me.GetAboutHeader());

        if (description.IsNotNullOrEmpty())
        {
            htmlMessage.Br()
                .Text(enginesConfig.Description)
                .Br();
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

        telegramService.ChatService.DeleteMessageHistory(
                history =>
                    history.MessageFlag == MessageFlag.About &&
                    history.ChatId == chatId,
                skipLast: 2
            )
            .InBackground();
    }

    public static async Task SendAboutUsernameAsync(this TelegramService telegramService)
    {
        var urlStart = await telegramService.GetUrlStart("start=set-username");
        var usernameStr = telegramService.IsNoUsername ? "belum" : "sudah";
        var sendText = "Tentang Username" +
                       $"\nKamu {usernameStr} mengatur Username";

        var inlineKeyboard = new InlineKeyboardMarkup(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithUrl("Cara Pasang Username", urlStart)
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Verifikasi Username", "verify username-only")
                }
            }
        );

        await telegramService.SendTextMessageAsync(
            sendText: sendText,
            replyMarkup: inlineKeyboard,
            scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
            includeSenderMessage: true,
            preventDuplicateSend: true
        );
    }
}
