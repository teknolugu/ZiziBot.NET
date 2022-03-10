using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Group;

public class PromoteCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public PromoteCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var message = _telegramService.MessageOrEdited;
        if (message.ReplyToMessage != null)
        {
            message = message.ReplyToMessage;
        }

        var userId = message.From.Id;
        var nameLink = message.GetFromNameLink();

        if (await _telegramService.CheckFromAdmin(userId))
        {
            await _telegramService.SendTextMessageAsync($"{nameLink} sudah menjadi Admin");
            return;
        }

        var sendText = $"{nameLink} berhasil menjadi Admin";

        var promote = await _telegramService.PromoteChatMemberAsync(userId);
        if (!promote.IsSuccess)
        {
            var errorCode = promote.ErrorCode;
            var errorMessage = promote.ErrorMessage;

            sendText = $"Promote {nameLink} gagal" +
                       $"\nPesan: {errorMessage}";
        }

        await _telegramService.SendTextMessageAsync(sendText);
    }
}