using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Humanizer;
using Serilog;
using SerilogTimings;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Group;

public class NewChatMembersHandler : IUpdateHandler
{
    private readonly SettingsService _settingsService;
    private readonly TelegramService _telegramService;
    private readonly NewChatMembersService _newChatMembersService;

    public NewChatMembersHandler(
        AntiSpamService antiSpamService,
        IBackgroundJobClient jobClient,
        SettingsService settingsService,
        TelegramService telegramService,
        StepHistoriesService stepHistoriesService,
        NewChatMembersService newChatMembersService
    )
    {
        _settingsService = settingsService;
        _telegramService = telegramService;
        _newChatMembersService = newChatMembersService;
    }

    public async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        CancellationToken cancellationToken
    )
    {
        var stopwatch = Stopwatch.StartNew();

        await _telegramService.AddUpdateContext(context);

        var msg = _telegramService.Message;
        var chatId = _telegramService.ChatId;
        var chatTitle = _telegramService.ChatTitle;

        var op = Operation.Begin("New Chat Members on ChatId {ChatId}", chatId);

        var chatSetting = await _settingsService.GetSettingsByGroup(chatId);

        if (!chatSetting.EnableWelcomeMessage)
        {
            Log.Information("Welcome message is disabled at ChatId: {ChatId}", chatId);
            return;
        }

        var welcomeMessage = chatSetting.WelcomeMessage;
        var welcomeButton = chatSetting.WelcomeButton;

        var newMembers = msg.NewChatMembers;

        if (newMembers == null) return;

        var isBootAdded = await _telegramService.IsAnyMe(newMembers);

        if (isBootAdded)
        {
            var getMe = await _telegramService.GetMeAsync();

            var greetMe = $"Hai, perkenalkan saya {getMe.FirstName}" +
                          $"\n\nSaya adalah bot pendebug dan grup manajemen yang dilengkapi dengan alat keamanan. " +
                          $"Agar saya berfungsi penuh, jadikan saya admin dengan level standard. " +
                          $"\n\nUntuk melihat daftar perintah bisa ketikkan /help";

            await _telegramService.SendTextMessageAsync(greetMe, replyToMsgId: 0);

            await _settingsService.SaveSettingsAsync
            (
                new Dictionary<string, object>()
                {
                    { "chat_id", chatId },
                    { "chat_title", chatTitle }
                }
            );

            if (newMembers.Length == 1) return;
        }

        var parsedNewMember = await _newChatMembersService.CheckNewChatMembers
        (
            chatId, newMembers, answer =>
                _telegramService.CallbackAnswerAsync(answer)
        );

        var allNewMember = parsedNewMember.AllNewChatMembersStr.JoinStr(", ");
        var allNoUsername = parsedNewMember.NewNoUsernameChatMembersStr.JoinStr(", ");
        var allNewBot = parsedNewMember.NewBotChatMembersStr.JoinStr(", ");

        if (allNewMember.Length == 0)
        {
            Log.Information("Welcome Message ignored because User is Global Banned..");
            return;
        }

        var greet = TimeUtil.GetTimeGreet();
        var memberCount = await _telegramService.GetMemberCount();
        var newMemberCount = newMembers.Length;

        Log.Information("Preparing send Welcome..");

        if (welcomeMessage.IsNullOrEmpty())
        {
            welcomeMessage = "Hai {AllNewMember}" +
                             "\nSelamat datang di kontrakan {ChatTitle}" +
                             "\nKamu adalah anggota ke-{MemberCount}";
        }

        var sendText = welcomeMessage.ResolveVariable
        (
            new List<(string placeholder, string value)>()
            {
                ("AllNewMember", allNewMember),
                ("AllNoUsername", allNoUsername),
                ("AllNewBot", allNewBot),
                ("ChatTitle", chatTitle),
                ("Greet", greet),
                ("NewMemberCount", newMemberCount.ToString()),
                ("MemberCount", memberCount.ToString())
            }
        );

        var keyboard = InlineKeyboardMarkup.Empty();

        if (!welcomeButton.IsNullOrEmpty())
        {
            keyboard = welcomeButton.ToReplyMarkup(2);
        }

        if (chatSetting.EnableHumanVerification)
        {
            Log.Debug("Adding verify button..");

            const string verifyButton = $"Saya adalah Manusia!|verify";
            var withVerify = string.Join(",", welcomeButton, verifyButton);

            keyboard = withVerify.ToReplyMarkup(2);
        }

        Message sentMessage;

        Log.Debug("New Member handler before send. Time: {Elapsed}", stopwatch.Elapsed);

        if (chatSetting.WelcomeMediaType != MediaType.Unknown)
        {
            var welcomeMedia = chatSetting.WelcomeMedia;
            var mediaType = chatSetting.WelcomeMediaType;

            sentMessage = await _telegramService.SendMediaAsync
            (
                fileId: welcomeMedia,
                mediaType: mediaType,
                caption: sendText,
                replyMarkup: keyboard,
                replyToMsgId: 0
            );
        }
        else
        {
            sentMessage = await _telegramService.SendTextMessageAsync(sendText, keyboard, 0);
        }

        var prevMsgId = chatSetting.LastWelcomeMessageId.ToInt();

        if (prevMsgId > 0)
            await _telegramService.DeleteAsync(prevMsgId);

        await _settingsService.SaveSettingsAsync
        (
            new Dictionary<string, object>()
            {
                { "chat_id", _telegramService.ChatId },
                { "chat_title", _telegramService.ChatTitle },
                { "chat_type", _telegramService.Chat.Type.Humanize() },
                { "members_count", memberCount },
                { "last_welcome_message_id", sentMessage.MessageId },
            }
        );

        op.Complete();
    }
}