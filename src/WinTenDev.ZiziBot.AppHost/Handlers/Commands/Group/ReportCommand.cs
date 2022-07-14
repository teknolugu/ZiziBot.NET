using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.ReplyMarkups;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Group;

public class ReportCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public ReportCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);

        var msg = context.Update.Message;
        var sendText = "Balas pesan yg mau di report";

        if (_telegramService.IsPrivateChat) return;

        if (msg.ReplyToMessage != null)
        {
            var repMsg = msg.ReplyToMessage;

            if (msg.From.Id != repMsg.From.Id)
            {
                var mentionAdmins = await _telegramService.GetMentionAdminsStr();

                var reporterNameLink = msg.GetFromNameLink();
                var reportedNameLink = repMsg.GetFromNameLink();
                var repMsgLink = repMsg.GetMessageLink();

                sendText = $"Ada laporan nich." +
                           $"\n{reporterNameLink} melaporkan {reportedNameLink}" +
                           $"{mentionAdmins}";

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Hapus", "PONG"),
                        InlineKeyboardButton.WithCallbackData("Tendang", "PONG")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Ban", "PONG"),
                        InlineKeyboardButton.WithUrl("Ke Pesan", repMsgLink)
                    }
                });

                await _telegramService.SendTextMessageAsync(sendText)
                    ;
                return;
            }

            sendText = "Melaporkan diri sendiri? 🤔";
        }

        await _telegramService.SendTextMessageAsync(sendText);
    }
}
