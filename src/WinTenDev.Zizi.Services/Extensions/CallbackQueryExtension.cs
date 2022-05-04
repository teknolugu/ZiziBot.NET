using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Types.Enums;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.Zizi.Services.Extensions;

public static class CallbackQueryExtension
{
    public static async Task<bool> OnCallbackPingAsync(this TelegramService telegramService)
    {
        Log.Information("Receiving Ping callback");
        var callbackQuery = telegramService.CallbackQuery;

        var callbackData = callbackQuery.Data;
        Log.Debug("CallbackData: {CallbackData}", callbackData);

        var answerCallback = $"Callback: {callbackData}";

        await telegramService.AnswerCallbackQueryAsync(answerCallback, showAlert: true);

        return true;
    }

    public static async Task<bool> OnCallbackVerifyAsync(this TelegramService telegramService)
    {
        Log.Information("Executing Verify Callback");

        var callbackQuery = telegramService.CallbackQuery;
        var callbackData = callbackQuery.Data;
        var fromId = telegramService.FromId;
        var chatId = telegramService.ChatId;

        var stepHistoriesService = telegramService.GetRequiredService<StepHistoriesService>();
        var userProfilePhotoService = telegramService.GetRequiredService<UserProfilePhotoService>();

        Log.Debug(
            "CallbackData: {CallbackData} from {FromId}",
            callbackData,
            fromId
        );

        var partCallbackData = callbackData.Split(" ");
        var callBackParam1 = partCallbackData.ElementAtOrDefault(1);
        var answer = "Tombol ini bukan untukmu Bep!";

        Log.Debug("Verify Param1: {Param}", callBackParam1);

        Log.Information("Starting Verify from History for UserId: {UserId}", fromId);
        var needVerifyList = (await stepHistoriesService.GetStepHistoryVerifyCore(
            new StepHistory()
            {
                ChatId = chatId,
                UserId = fromId
            }
        )).ToList();

        if (!needVerifyList.Any())
        {
            answer = "Kamu tidak perlu verifikasi!";
        }
        else
        {
            await userProfilePhotoService.ResetUserProfilePhotoCacheAsync(fromId);

            foreach (var step in needVerifyList)
            {
                var updateHistory = step;
                updateHistory.UpdatedAt = DateTime.Now;

                switch (step.Name)
                {
                    case StepHistoryName.ChatMemberUsername:
                        Log.Debug("Verifying Username for UserId {UserId}", fromId);
                        if (telegramService.HasUsername)
                        {
                            updateHistory.Status = StepHistoryStatus.HasVerify;
                        }
                        break;

                    case StepHistoryName.ChatMemberPhoto:
                        Log.Debug("Verifying User Profile Photo for UserId {UserId}", fromId);
                        if (await userProfilePhotoService.HasUserProfilePhotosAsync(fromId))
                        {
                            updateHistory.Status = StepHistoryStatus.HasVerify;
                        }
                        break;

                    case StepHistoryName.ForceSubscription:
                        var chatMember = await telegramService.ChatService.GetChatMemberAsync(
                            chatId: chatId,
                            userId: fromId,
                            evictAfter: true
                        );

                        if (chatMember.Status != ChatMemberStatus.Left)
                            updateHistory.Status = StepHistoryStatus.HasVerify;

                        break;

                    case StepHistoryName.HumanVerification:
                        updateHistory.Status = StepHistoryStatus.HasVerify;
                        break;

                    default:
                        break;
                }

                await stepHistoriesService.SaveStepHistory(updateHistory);
            }

            var afterVerify = await stepHistoriesService.GetStepHistoryVerifyCore(
                new StepHistory()
                {
                    ChatId = chatId,
                    UserId = fromId
                }
            );

            if (!afterVerify.Any())
            {
                await telegramService.UnmuteChatMemberAsync(fromId);
                answer = "Terima kasih sudah verifikasi!";
            }
            else
            {
                answer = "Silakan lakukan sesuai instruksi, lalu tekan Verifikasi";
            }
        }

        await telegramService.AnswerCallbackQueryAsync(answer);
        return true;
    }

    public static async Task<bool> OnCallbackRssCtlAsync(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;
        var chatTitle = telegramService.ChatTitle;
        var messageId = telegramService.CallBackMessageId;
        var answerHeader = $"RSS Control for {chatTitle}";
        var answerDescription = string.Empty;
        var part = telegramService.CallbackQuery.Data?.Split(" ");
        var rssId = part!.ElementAtOrDefault(2);
        var page = 0;
        const int take = 5;

        if (!await telegramService.CheckUserPermission())
        {
            await telegramService.AnswerCallbackQueryAsync("Anda tidak mempunyai akses", true);

            return false;
        }

        var rssService = telegramService.GetRequiredService<RssService>();
        var rssFeedService = telegramService.GetRequiredService<RssFeedService>();
        var messageHistoryService = telegramService.GetRequiredService<MessageHistoryService>();

        var rssFind = new RssSettingFindDto()
        {
            ChatId = chatId
        };

        var updateValue = new Dictionary<string, object>()
        {
            { "updated_at", DateTime.UtcNow }
        };

        switch (part.ElementAtOrDefault(1))
        {
            case "stop-all":
                updateValue.Add("is_enabled", false);
                answerDescription = $"Semua service berhasil dimatikan";
                break;

            case "start-all":
                updateValue.Add("is_enabled", true);
                answerDescription = "Semua service berhasil diaktifkan";
                break;

            case "start":
                rssFind.Id = rssId.ToInt64();
                updateValue.Add("is_enabled", true);
                answerDescription = "Service berhasil di aktifkan";
                break;

            case "stop":
                rssFind.Id = rssId.ToInt64();
                updateValue.Add("is_enabled", false);
                answerDescription = "Service berhasil dimatikan";
                break;

            case "attachment-off":
                rssFind.Id = rssId.ToInt64();
                updateValue.Add("include_attachment", false);
                answerDescription = "Attachment tidak akan ditambahkan";
                break;

            case "attachment-on":
                rssFind.Id = rssId.ToInt64();
                updateValue.Add("include_attachment", true);
                answerDescription = "Berhasil mengaktifkan attachment";
                break;

            case "delete":
                await rssService.DeleteRssAsync(
                    chatId: chatId,
                    columnName: "id",
                    columnValue: rssId
                );
                answerDescription = "Service berhasil dihapus";
                break;

            case "navigate-page":
                var toPage = part.ElementAtOrDefault(2).ToInt();
                page = toPage;
                answerDescription = "Halaman " + (toPage + 1);
                break;
        }

        await rssService.UpdateRssSettingAsync(rssFind, updateValue);

        await rssFeedService.ReRegisterRssFeedByChatId(chatId);

        var answerCombined = answerHeader + Environment.NewLine + answerDescription;

        var btnMarkupCtl = await rssService.GetButtonMarkup(
            chatId: chatId,
            page: page,
            take: take
        );

        if (answerDescription.IsNotNullOrEmpty())
        {
            await telegramService.EditMessageCallback(answerCombined, btnMarkupCtl);

            if (part.ElementAtOrDefault(1)?.Contains("all") ?? false)
                await telegramService.AnswerCallbackQueryAsync(answerCombined, true);
        }

        await messageHistoryService.UpdateDeleteAtAsync(
            new MessageHistoryFindDto()
            {
                ChatId = chatId,
                MessageId = messageId
            },
            DateTime.UtcNow.AddMinutes(10)
        );

        return true;
    }

    public static async Task<bool> OnCallbackSettingAsync(this TelegramService telegramService)
    {
        var callbackQuery = telegramService.CallbackQuery;
        var chatId = callbackQuery.Message.Chat.Id;
        var fromId = callbackQuery.From.Id;
        var msgId = callbackQuery.Message.MessageId;

        if (!await telegramService.CheckUserPermission())
        {
            Log.Information(
                "UserId: {UserId} don't have permission at {ChatId}",
                fromId,
                chatId
            );
            return false;
        }

        Log.Information("Processing Setting Callback");
        var settingsService = telegramService.GetRequiredService<SettingsService>();

        var callbackData = callbackQuery.Data;
        var partedData = callbackData.Split(" ");
        var callbackParam = partedData.ValueOfIndex(1);
        var partedParam = callbackParam.Split("_");
        var valueParamStr = partedParam.ValueOfIndex(0);
        var keyParamStr = callbackParam.Replace(valueParamStr, "");
        var currentVal = valueParamStr.ToBoolInt();

        Log.Information("Param : {KeyParamStr}", keyParamStr);
        Log.Information("CurrentVal : {CurrentVal}", currentVal);

        var columnTarget = "enable" + keyParamStr;
        var newValue = currentVal == 0 ? 1 : 0;

        Log.Information(
            "Column: {ColumnTarget}, Value: {CurrentVal}, NewValue: {NewValue}",
            columnTarget,
            currentVal,
            newValue
        );

        var data = new Dictionary<string, object>()
        {
            ["chat_id"] = chatId,
            [columnTarget] = newValue
        };

        await settingsService.SaveSettingsAsync(data);

        var settingBtn = await settingsService.GetSettingButtonByGroup(chatId);
        var btnMarkup = await settingBtn.ToJson().JsonToButton(chunk: 2);
        Log.Debug("Settings: {Count}", settingBtn.Count);

        telegramService.SentMessageId = msgId;

        var editText = $"Settings Toggles" +
                       $"\nParam: {columnTarget} to {newValue}";
        await telegramService.EditMessageCallback(editText, btnMarkup);

        await settingsService.UpdateCacheAsync(chatId);

        return true;
    }

    public static async Task<bool> OnCallbackGlobalBanAsync(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;
        var fromId = telegramService.FromId;
        var message = telegramService.CallbackMessage;
        var callbackDatas = telegramService.CallbackQueryDatas;

        if (!await telegramService.CheckFromAdminOrAnonymous())
        {
            Log.Debug(
                "UserId: {UserId} is not admin at ChatId: {ChatId}",
                fromId,
                chatId
            );

            return false;
        }

        var globalBanService = telegramService.GetRequiredService<GlobalBanService>();
        var eventLogService = telegramService.GetRequiredService<EventLogService>();

        var action = callbackDatas.ElementAtOrDefault(1);
        var userId = callbackDatas.ElementAtOrDefault(2).ToInt64();

        var replyToMessageId = telegramService.ReplyToMessage?.MessageId ?? -1;

        var answerCallback = string.Empty;

        var messageLog = HtmlMessage.Empty
            .TextBr("Global Ban di tambahkan baru")
            .Bold("Ban By: ").CodeBr(fromId.ToString())
            .Bold("UserId: ").CodeBr(userId.ToString());

        switch (action)
        {
            case "add":
                await globalBanService.SaveBanAsync(
                    new GlobalBanItem
                    {
                        UserId = userId,
                        ReasonBan = "@WinTenDev",
                        BannedBy = fromId,
                        BannedFrom = chatId,
                        CreatedAt = DateTime.UtcNow
                    }
                );

                await globalBanService.UpdateCache(userId);

                await telegramService.KickMemberAsync(userId, untilDate: DateTime.Now.AddSeconds(30));

                await eventLogService.SendEventLogAsync(
                    chatId: chatId,
                    message: message,
                    text: messageLog.ToString(),
                    forwardMessageId: replyToMessageId,
                    deleteForwardedMessage: true,
                    messageFlag: MessageFlag.GBan
                );

                await telegramService.DeleteMessageManyAsync(customUserId: userId);

                answerCallback = "Berhasil Memblokir Pengguna!";

                break;

            case "del":
                await globalBanService.DeleteBanAsync(userId);
                await globalBanService.UpdateCache(userId);

                answerCallback = "Terima kasih atas konfirmasinya!";

                break;
        }

        await telegramService.AnswerCallbackQueryAsync(answerCallback, true);

        return true;
    }
}
