using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace WinTenDev.Zizi.Services.Telegram;

public class NewChatMembersService
{
    private readonly ILogger<NewChatMembersService> _logger;
    private readonly AntiSpamService _antiSpamService;
    private readonly SettingsService _settingsService;
    private ChatSetting _chatSetting;

    public NewChatMembersService(
        ILogger<NewChatMembersService> logger,
        AntiSpamService antiSpamService,
        SettingsService settingsService
    )
    {
        _logger = logger;
        _antiSpamService = antiSpamService;
        _settingsService = settingsService;
    }

    public async Task<NewChatMembers> CheckNewChatMembers(
        long chatId,
        User[] users,
        Func<CallbackAnswer, Task<CallbackResult>> funcCallbackAnswer
    )
    {
        _chatSetting = await _settingsService.GetSettingsByGroup(chatId);

        var newChatMembers = new NewChatMembers
        {
            AllNewChatMembers = users.AsEnumerable()
        };

        _logger.LogDebug("Parsing new {Length} members..", users.Length);
        foreach (var user in users)
        {
            var callbackAnswerParam = new CallbackAnswer();
            var userId = user.Id;

            var banResult = await _antiSpamService.CheckSpam(chatId, userId);

            callbackAnswerParam.TargetUserId = userId;

            if (banResult.IsAnyBanned)
            {
                newChatMembers.NewKickedChatMembers.Add(user);
                callbackAnswerParam.CallbackAnswerModes.Add(CallbackAnswerMode.KickMember);
            }
            else
            {
                if (_chatSetting.EnableHumanVerification)
                {
                    newChatMembers.NewPassedChatMembers.Add(user);
                    callbackAnswerParam.CallbackAnswerModes.Add(CallbackAnswerMode.MuteMember);
                    callbackAnswerParam.CallbackAnswerModes.Add(CallbackAnswerMode.ScheduleKickMember);
                }

                newChatMembers.NewPassedChatMembers.Add(user);
            }

            await funcCallbackAnswer(callbackAnswerParam);
        }

        newChatMembers.AllNewChatMembersStr = users.Select(user => user.GetNameLink());
        newChatMembers.NewPassedChatMembersStr = newChatMembers.NewPassedChatMembers.Select(user => user.GetNameLink());

        newChatMembers.NewNoUsernameChatMembers = users.Where(user => user.Username.IsNullOrEmpty());
        newChatMembers.NewNoUsernameChatMembersStr = newChatMembers.NewNoUsernameChatMembers.Select(user => user.GetNameLink());

        newChatMembers.NewBotChatMembers = users.Where(user => user.IsBot);
        newChatMembers.NewBotChatMembersStr = newChatMembers.NewBotChatMembers.Select(user => user.GetNameLink());

        return newChatMembers;
    }
}