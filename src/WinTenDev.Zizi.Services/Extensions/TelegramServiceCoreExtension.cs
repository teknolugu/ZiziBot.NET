using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SerilogTimings;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace WinTenDev.Zizi.Services.Extensions;

public static class TelegramServiceCoreExtension
{
    public static TService GetRequiredService<TService>(this TelegramService telegramService)
    {
        var serviceScope = InjectionUtil.GetScope();
        var serviceProvider = serviceScope.ServiceProvider;
        var resolvedService = serviceProvider.GetRequiredService<TService>();

        return resolvedService;
    }

    public static void ResetCooldownByFeatureName(
        this TelegramService telegramService,
        string featureName = null
    )
    {
        var rateLimitingInMemory = telegramService.GetRequiredService<RateLimitingInMemory>();

        var commandName = featureName ?? telegramService.GetCommand();

        rateLimitingInMemory.FeatureCooldowns.Delete(
            new FeatureCooldown()
            {
                FeatureName = commandName,
            }
        );

        rateLimitingInMemory.FeatureCooldowns.Delete(
            new FeatureCooldown()
            {
                LastUsed = DateTime.UtcNow.AddMonths(-1)
            }
        );
    }

    public static async Task<bool> CheckProbeRequirementAsync(
        this TelegramService telegramService,
        bool checkAdmin = false
    )
    {
        var chatId = telegramService.ChatId;
        var wTelegramApiService = telegramService.GetRequiredService<WTelegramApiService>();

        if (checkAdmin)
        {
            var isProbeAdmin = await wTelegramApiService.IsProbeAdminAsync(chatId);
            if (isProbeAdmin) return true;
        }
        else
        {
            var isProbeHere = await wTelegramApiService.IsProbeHereAsync(chatId);
            if (isProbeHere) return true;
        }

        var probeInfo = await wTelegramApiService.GetMeAsync();
        var userId = probeInfo.full_user.id;
        var userName = probeInfo.users.FirstOrDefault(user => user.Key == userId).Value;

        var htmlMessage = HtmlMessage.Empty;

        htmlMessage.Text(
                checkAdmin
                    ? "Untuk dapat bekerja, ZiziBot membutuhkan Probe sebagai pembantu dalam menjalankan sebuah fitur. "
                    : "Karena ini bukan Grup Publik, ZiziBot membutuhkan Probe sebagai pembantu dalam menjalankan sebuah fitur. "
            )
            .Text("Adapun Probe untuk ZiziBot adalah ")
            .User(userId, userName.GetFullName()).Text(". ")
            .Text(
                checkAdmin
                    ? "Silakan tambahkan Pengguna tersebut ke Grup Anda dan jadikan sebagai Admin."
                    : "Silakan tambahkan Pengguna tersebut ke Grup Anda."
            );

        await telegramService.SendTextMessageAsync(
            sendText: htmlMessage.ToString(),
            scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
            includeSenderMessage: true
        );

        return false;
    }

    public static async Task BanSenderChatAsync(
        this TelegramService telegramService,
        long senderChatId
    )
    {
        var chatId = telegramService.ChatId;

        try
        {
            Log.Information("Banning {SenderChatId} from {ChatId}", senderChatId, chatId);
            await telegramService.Client.BanChatSenderChatAsync(chatId, senderChatId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while banning {SenderChatId} from {ChatId}", senderChatId, chatId);
        }
    }

    public static async Task SendStartAsync(this TelegramService telegramService)
    {
        var enginesConfig = telegramService.EnginesConfig;
        var rulesProcessor = telegramService.GetRequiredService<RulesProcessor>();
        var usernameProcessor = telegramService.GetRequiredService<UsernameProcessor>();
        var botService = telegramService.GetRequiredService<BotService>();

        var msg = telegramService.Message;
        var partText = msg.Text.SplitText(" ").ToArray();
        var startArg = partText.ElementAtOrDefault(1);
        var startArgs = startArg?.Split("_");
        var startCmd = startArgs?.FirstOrDefault();

        Log.Debug("Start Args: {StartArgs}", startArgs);

        var featureConfig = await telegramService.GetFeatureConfig();

        var getMe = await botService.GetMeAsync();
        var urlAddTo = await botService.GetUrlStart("startgroup=new");
        var aboutHeader = getMe.GetAboutHeader();
        var botDescription = enginesConfig.Description;

        var startHeader = $"🤖 {aboutHeader}" +
                          $"\n{botDescription}." +
                          $"\n\n";

        var sendText = $"Adalah bot debugging dan manajemen grup yang dilengkapi dengan alat keamanan.";

        var result = startCmd switch
        {
            "rules" => await rulesProcessor.Execute(startArgs.ElementAtOrDefault(1)),
            "set-username" => usernameProcessor.Execute(startArgs.ElementAtOrDefault(1)),
            "sub-dl" => await telegramService.OnStartSubsceneDownloadAsync(startArgs.ElementAtOrDefault(1)),
            "yt-dl" => await telegramService.OnStartYoutubeDownloadAsync(),
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
        var caption = startHeader + (featureConfig.Caption ?? sendText);

        var replyMarkup = featureConfig.KeyboardButton;

        replyMarkup.Add(
            new[]
            {
                InlineKeyboardButton.WithUrl("Tambahkan ke Grup", urlAddTo)
            }
        );

        var keyboard = replyMarkup.ToButtonMarkup();

        await telegramService.SendTextMessageAsync(
            sendText: caption,
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

    public static async Task GetOutAsync(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;
        var partsMsg = telegramService.MessageTextParts;
        var client = telegramService.Client;

        await telegramService.DeleteSenderMessageAsync();

        if (!telegramService.IsFromSudo) return;

        var sendText = "Maaf, saya harus keluar";

        if (partsMsg.ElementAtOrDefault(2) != null)
        {
            sendText += $"\n{partsMsg.ElementAtOrDefault(2)}";
        }

        var message = telegramService.Message;
        var chatService = telegramService.GetRequiredService<ChatService>();
        var eventLogService = telegramService.GetRequiredService<EventLogService>();

        var targetChatId = partsMsg.ElementAtOrDefault(1).ToInt64();
        Log.Information("Target out: {ChatId}", targetChatId);

        var me = await telegramService.GetMeAsync();
        var meFullName = me.GetFullName();

        try
        {
            if (targetChatId == 0) targetChatId = chatId;

            var isMeHere = await chatService.IsMeHereAsync(targetChatId);

            if (!isMeHere)
            {
                await telegramService.SendTextMessageAsync(
                    sendText: $"Sepertinya {meFullName} bukan lagi anggota grub {targetChatId}",
                    disableWebPreview: true,
                    scheduleDeleteAt: DateTime.UtcNow.AddMinutes(3),
                    includeSenderMessage: true
                );

                return;
            }

            await telegramService.SendTextMessageAsync(
                sendText,
                customChatId: targetChatId,
                replyToMsgId: 0
            );

            await client.LeaveChatAsync(targetChatId);

            if (targetChatId != chatId)
            {
                var chatInfo = await chatService.GetChatAsync(targetChatId);
                var memberCount = await chatService.GetMemberCountAsync(targetChatId);

                var htmlMessage = HtmlMessage.Empty
                    .Bold(meFullName).TextBr(" berhasil keluar dari Grup")
                    .Bold("ChatId: ").CodeBr(targetChatId.ToString())
                    .Bold("Name: ").TextBr(chatInfo.GetChatNameLink())
                    .Bold("Member Count: ").Code(memberCount.ToString());

                await telegramService.SendTextMessageAsync(
                    sendText: htmlMessage.ToString(),
                    disableWebPreview: true,
                    scheduleDeleteAt: DateTime.UtcNow.AddDays(3)
                );

                await eventLogService.SendEventLogAsync(
                    message: message,
                    text: htmlMessage.ToString(),
                    chatId: chatId,
                    messageFlag: MessageFlag.Leave,
                    sendGlobalOnly: true
                );
            }
        }
        catch (Exception e)
        {
            await telegramService.SendTextMessageAsync(
                sendText: $"Sepertinya {meFullName} bukan lagi anggota ChatId {targetChatId}" +
                          $"\nMessage: {e.Message}",
                scheduleDeleteAt: DateTime.UtcNow.AddDays(3)
            );
        }
    }

    public static async Task GetAppHostInfoAsync(this TelegramService telegramService)
    {
        var featureConfig = await telegramService.GetFeatureConfig();

        if (!featureConfig.NextHandler)
        {
            return;
        }

        var appHostInfo = AppHostUtil.GetAppHostInfo(includeUptime: true);

        await telegramService.SendTextMessageAsync(
            sendText: appHostInfo,
            includeSenderMessage: true,
            scheduleDeleteAt: DateTime.UtcNow.AddMinutes(10)
        );
    }
}