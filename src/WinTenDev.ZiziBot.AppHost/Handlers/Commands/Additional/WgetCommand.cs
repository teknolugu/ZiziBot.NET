using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Externals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Additional;

public class WgetCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly MegaApiService _megamapiService;

    public WgetCommand(
        TelegramService telegramService,
        MegaApiService megamapiService
    )
    {
        _telegramService = telegramService;
        _megamapiService = megamapiService;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);

        var message = _telegramService.Message;
        var messageText = _telegramService.Message.Text.GetTextWithoutCmd();
        var chatId = message.Chat.Id;
        var partsText = messageText.Split(" ");
        var param1 = partsText.ValueOfIndex(0);

        var isBeta = await _telegramService.IsBeta();

        if (!_telegramService.IsFromSudo)
        {
            if (isBeta)
            {
                await _telegramService.SendTextMessageAsync("Fitur Wget saat ini masih dibatasi.");
                return;
            }

            if (chatId != -1001272521285)
            {
                await _telegramService.SendTextMessageAsync("Fitur Wget dapat di gunakan di grup @WinTenMirror");
                return;
            }
        }

        if (param1.IsNullOrEmpty())
        {
            await _telegramService.SendTextMessageAsync("Silakan sertakan tautan yang akan di download");
            return;
        }

        await _telegramService.SendTextMessageAsync($"Preparing download file " +
                                                    $"\nUrl: {param1}");

        if (param1.IsMegaUrl())
        {
            await _megamapiService.DownloadFileAsync(param1, answer => {
                return _telegramService.AnswerCallbackAsync(answer);
            });
        }
        else if (param1.IsUptoboxUrl())
        {
            // await _telegramService.DownloadUrlAsync();
        }
        else
        {
            // await _telegramService.DownloadFile(param1);
        }
    }
}