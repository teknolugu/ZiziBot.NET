using System;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Types;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Callbacks;

public class VerifyCallback
{
    private readonly TelegramService _telegramService;
    private readonly UserProfilePhotoService _userProfilePhotoService;
    private readonly StepHistoriesService _stepHistoriesService;
    private readonly CallbackQuery _callbackQuery;

    public VerifyCallback(
        TelegramService telegramService,
        UserProfilePhotoService userProfilePhotoService,
        StepHistoriesService stepHistoriesService
    )
    {
        _telegramService = telegramService;
        _userProfilePhotoService = userProfilePhotoService;
        _stepHistoriesService = stepHistoriesService;
        _callbackQuery = telegramService.CallbackQuery;

        Log.Information("Receiving Verify Callback");
    }

    public async Task<bool> ExecuteVerifyAsync()
    {
        Log.Information("Executing Verify Callback");

        var callbackData = _callbackQuery.Data;
        var fromId = _telegramService.FromId;
        var chatId = _telegramService.ChatId;

        Log.Debug("CallbackData: {CallbackData} from {FromId}", callbackData, fromId);

        var partCallbackData = callbackData.Split(" ");
        var callBackParam1 = partCallbackData.ElementAtOrDefault(1);
        var answer = "Tombol ini bukan untukmu Bep!";

        Log.Debug("Verify Param1: {0}", callBackParam1);

        Log.Information("Starting Verify from History for UserId: {UserId}", fromId);
        var needVerifyList = await _stepHistoriesService.GetStepHistoryVerifyCore(new StepHistory()
        {
            ChatId = chatId,
            UserId = fromId
        });

        if (!needVerifyList.Any())
        {
            answer = "Kamu tidak perlu verifikasi!";
        }
        else
        {
            await _userProfilePhotoService.ResetUserProfilePhotoCacheAsync(fromId);

            foreach (var step in needVerifyList)
            {
                var updateHistory = step;
                updateHistory.UpdatedAt = DateTime.Now;

                switch (step.Name)
                {
                    case StepHistoryName.ChatMemberUsername:
                        Log.Debug("Verifying Username for UserId {UserId}", fromId);
                        if (_telegramService.HasUsername)
                        {
                            updateHistory.Status = StepHistoryStatus.HasVerify;
                        }
                        break;

                    case StepHistoryName.ChatMemberPhoto:
                        Log.Debug("Verifying User Profile Photo for UserId {UserId}", fromId);
                        if (await _userProfilePhotoService.HasUserProfilePhotosAsync(fromId))
                        {
                            updateHistory.Status = StepHistoryStatus.HasVerify;
                        }
                        break;

                    case StepHistoryName.HumanVerification:
                        updateHistory.Status = StepHistoryStatus.HasVerify;
                        break;

                    default:
                        break;
                }

                await _stepHistoriesService.SaveStepHistory(updateHistory);
            }

            var afterVerify = await _stepHistoriesService.GetStepHistoryVerifyCore(new StepHistory()
            {
                ChatId = chatId,
                UserId = fromId
            });

            if (!afterVerify.Any())
            {
                await _telegramService.UnmuteChatMemberAsync(fromId);
                answer = "Terima kasih sudah verifikasi!";
            }
            else
            {
                answer = "Silakan lakukan sesuai instruksi, lalu tekan Verifikasi";
            }
        }

        await _telegramService.AnswerCallbackQueryAsync(answer);
        return true;
    }
}