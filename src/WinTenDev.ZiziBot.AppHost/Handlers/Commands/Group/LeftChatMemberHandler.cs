using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Group;

public class LeftChatMemberHandler : IUpdateHandler
{
    private readonly TelegramService _telegramService;
    private readonly AntiSpamService _antiSpamService;

    public LeftChatMemberHandler(
        TelegramService telegramService,
        AntiSpamService antiSpamService
    )
    {
        _telegramService = telegramService;
        _antiSpamService = antiSpamService;
    }

    public async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        CancellationToken cancellationToken
    )
    {
        await _telegramService.AddUpdateContext(context);

        var chatId = _telegramService.ChatId;
        var chatTitle = _telegramService.ChatTitle;

        var msg = _telegramService.Message;
        var leftMember = msg.LeftChatMember;

        if (leftMember == null) return;

        var leftUserId = leftMember.Id;
        var checkSpam = await _antiSpamService.CheckSpam(chatId, leftUserId);

        if (checkSpam.IsAnyBanned)
        {
            Log.Information("Left Message ignored because {LeftMember} is Global Banned", leftMember);
            return;
        }

        Log.Information("Left Chat Members...");

        var leftFullName = leftMember.FirstName;

        var sendText = $"Sampai jumpa lagi {leftFullName} " +
                       $"\nKami di <b>{chatTitle}</b> menunggumu kembali.. :(";

        await _telegramService.SendTextMessageAsync(sendText, replyToMsgId: 0);
    }
}
