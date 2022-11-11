using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Serilog;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace WinTenDev.Zizi.Services.Callbacks;

public class VerifyCallback
{
    private readonly IMapper _mapper;
    private readonly TelegramService _telegramService;
    private readonly UserProfilePhotoService _userProfilePhotoService;
    private readonly StepHistoriesService _stepHistoriesService;
    private readonly CallbackQuery _callbackQuery;

    public VerifyCallback(
        IMapper mapper,
        TelegramService telegramService,
        UserProfilePhotoService userProfilePhotoService,
        StepHistoriesService stepHistoriesService
    )
    {
        _mapper = mapper;
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
        var needVerifyList = (await _stepHistoriesService.GetStepHistoryVerifyCore(
            new StepHistoryDto()
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
            await _userProfilePhotoService.ResetUserProfilePhotoCacheAsync(fromId);

            foreach (var step in needVerifyList)
            {
                var stepHistoryDto = new StepHistoryDto();

                stepHistoryDto = _mapper.Map<StepHistoryDto>(step);
                // updateHistory.UpdatedAt = DateTime.Now;

                switch (step.Name)
                {
                    case StepHistoryName.ChatMemberUsername:
                        Log.Debug("Verifying Username for UserId {UserId}", fromId);
                        if (_telegramService.HasUsername)
                        {
                            stepHistoryDto.Status = StepHistoryStatus.HasVerify;
                        }
                        break;

                    case StepHistoryName.ChatMemberPhoto:
                        Log.Debug("Verifying User Profile Photo for UserId {UserId}", fromId);
                        if (await _userProfilePhotoService.HasUserProfilePhotosAsync(fromId))
                        {
                            stepHistoryDto.Status = StepHistoryStatus.HasVerify;
                        }
                        break;

                    case StepHistoryName.ForceSubscription:
                        var chatMember = await _telegramService.ChatService.GetChatMemberAsync(
                            chatId: chatId,
                            userId: fromId,
                            evictAfter: true
                        );

                        if (chatMember.Status != ChatMemberStatus.Left)
                            stepHistoryDto.Status = StepHistoryStatus.HasVerify;

                        break;

                    case StepHistoryName.HumanVerification:
                        stepHistoryDto.Status = StepHistoryStatus.HasVerify;
                        break;

                    default:
                        break;
                }

                await _stepHistoriesService.SaveStepHistory(stepHistoryDto);
            }

            var afterVerify = await _stepHistoriesService.GetStepHistoryVerifyCore(
                new StepHistoryDto()
                {
                    ChatId = chatId,
                    UserId = fromId
                }
            );

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