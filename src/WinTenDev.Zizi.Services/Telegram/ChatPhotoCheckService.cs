using System;
using System.Linq;
using System.Threading.Tasks;
using SerilogTimings;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Telegram;

public class ChatPhotoCheckService
{
    private readonly ChatService _chatService;
    private readonly PrivilegeService _privilegeService;
    private readonly StepHistoriesService _stepHistoriesService;

    public ChatPhotoCheckService(
        ChatService chatService,
        PrivilegeService privilegeService,
        StepHistoriesService stepHistoriesService
    )
    {
        _chatService = chatService;
        _privilegeService = privilegeService;
        _stepHistoriesService = stepHistoriesService;
    }

    public async Task<bool> CheckChatPhoto(long chatId, long userId, Func<CallbackAnswer, Task> funcCallbackAnswer = null)
    {
        var op = Operation.Begin("Check Chat Photo for UserId: {UserId}", userId);
        var userProfilePhotos = await _chatService.GetChatPhotoAsync(userId);
        var hasPhoto = userProfilePhotos.TotalCount > 0;

        var adminOrPrivate = await _privilegeService.IsAdminOrPrivateChat(chatId, userId);

        if (funcCallbackAnswer == null || hasPhoto || adminOrPrivate)
        {
            op.Complete();

            return true;
        }

        var lastStep = await GetLastStep(chatId, userId);
        var member = await _chatService.GetChatMemberAsync(chatId, userId);
        var fullName = member.User.GetFullName();

        await funcCallbackAnswer(new CallbackAnswer
        {
            CallbackAnswerText = $"Hai {fullName}" +
                                 "\nKamu belum mengatur/menyembunyikan Poto profil, silakan atur Poto profil yak. " +
                                 "Jika sudah atur Poto, silakan tekan tombol dibawah ini untuk verifikasi. " +
                                 $"\nIni peringatan ke {lastStep.StepCount}",
            MuteMemberTimeSpan = TimeSpan.FromMinutes(1),
            CallbackAnswerModes = new[]
            {
                CallbackAnswerMode.SendMessage,
                CallbackAnswerMode.BanMember,
                CallbackAnswerMode.MuteMember
            }
        });

        await NextStep(chatId, userId);

        op.Complete();

        return false;
    }

    private async Task NextStep(long chatId, long userId)
    {

    }

    private async Task<StepHistory> GetLastStep(long chatId, long userId)
    {
        var member = await _chatService.GetChatMemberAsync(chatId, userId);
        var user = member.User;

        var updatedHistory = new StepHistory
        {
            Name = "Photo profile Check",
            ChatId = chatId,
            UserId = userId,
            FirstName = user.FirstName ?? "",
            LastName = user.LastName ?? "",
            Reason = "User don't have photo profile",
            StepCount = 1,
            LastWarnMessageId = 1,
            UpdatedAt = DateTime.UtcNow
        };

        var stepHistories = await _stepHistoriesService.GetStepHistory(chatId);
        var stepHistory = stepHistories.FirstOrDefault(stepHistory => stepHistory.UserId == userId);

        if (stepHistory != null)
        {
            updatedHistory.StepCount = stepHistory.StepCount + 1;
            updatedHistory.CreatedAt = stepHistory.CreatedAt;
        }
        else
        {
            updatedHistory.LastWarnMessageId = -1000;
            updatedHistory.CreatedAt = DateTime.UtcNow;
        }

        await _stepHistoriesService.SaveStep(updatedHistory);

        return updatedHistory;
    }
}