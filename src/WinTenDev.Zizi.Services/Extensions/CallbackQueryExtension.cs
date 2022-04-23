using System;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Types.Enums;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;

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
}
