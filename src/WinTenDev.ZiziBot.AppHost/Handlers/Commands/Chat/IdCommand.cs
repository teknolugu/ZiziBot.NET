using System.Threading.Tasks;
using SerilogTimings;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Chat;

public class IdCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly PrivilegeService _privilegeService;

    public IdCommand(
        TelegramService telegramService,
        PrivilegeService privilegeService
    )
    {
        _telegramService = telegramService;
        _privilegeService = privilegeService;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);

        var chatTitle = _telegramService.Chat.Title;
        var chatId = _telegramService.ChatId;
        var chatType = _telegramService.Chat.Type;
        var userId = _telegramService.FromId;
        var fullName = _telegramService.From.GetFullName();
        var userLang = _telegramService.From?.LanguageCode;

        var op = Operation.Begin("Command '/id' on ChatId '{ChatId}'", chatId);

        var fromSudo = _telegramService.IsFromSudo;

        var text = $"👥 <b>{chatTitle}</b>\n" +
                   $"Chat ID: <code>{chatId}</code>\n" +
                   $"Type: <code>{chatType}</code>\n\n" +
                   $"👤 <b>{fullName}</b>\n" +
                   $"Is Sudo: <code>{fromSudo}</code>\n" +
                   $"User ID: <code>{userId}</code>\n" +
                   $"Language: <code>{userLang.ToUpperCase()}</code>";

        if (_telegramService.ReplyToMessage != null)
        {
            var repMsg = _telegramService.ReplyToMessage;
            var repToFromId = _telegramService.ReplyFromId;
            var repFullName = repMsg.From.GetFullName();
            var repToSudo = _privilegeService.IsFromSudo(repToFromId);

            text += $"\n\n👤 <b>{repFullName}</b>" +
                    $"\nIs Sudo: <code>{repToSudo}</code>" +
                    $"\nUser ID: <code>{repToFromId}</code>" +
                    $"\nLanguage: <code>{repMsg.From?.LanguageCode}</code>";
        }

        await _telegramService.SendTextMessageAsync(text);
        op.Complete();
    }
}