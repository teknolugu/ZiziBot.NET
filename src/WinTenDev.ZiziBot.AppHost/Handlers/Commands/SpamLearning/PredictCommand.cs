using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.SpamLearning;

public class PredictCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public PredictCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);

        var message = _telegramService.Message;

        if (message.ReplyToMessage != null)
        {
            var repMsg = message.ReplyToMessage;
            var repMsgText = repMsg.Text;

            await _telegramService.SendTextMessageAsync("Sedang memprediksi pesan")
                ;

            var isSpam = MachineLearning.PredictMessage(repMsgText);
            await _telegramService.EditMessageTextAsync($"IsSpam: {isSpam}");

            return;
        }
        else
        {
            await _telegramService.SendTextMessageAsync("Predicting message");
        }
    }
}