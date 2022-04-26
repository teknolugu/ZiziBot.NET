using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Types.Enums;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

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
}
