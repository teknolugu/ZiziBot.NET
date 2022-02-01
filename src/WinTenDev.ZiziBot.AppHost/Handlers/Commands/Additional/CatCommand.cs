using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Externals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Additional;

public class CatCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly AnimalsService _animalsService;

    public CatCommand(
        TelegramService telegramService,
        AnimalsService animalsService
    )
    {
        _telegramService = telegramService;
        _animalsService = animalsService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var message = _telegramService.Message;
        var partsText = message.Text.SplitText(" ").ToArray();
        var catNum = 1;
        var param1 = partsText.ElementAtOrDefault(1);

        if (_telegramService.IsCommand("/cats"))
        {
            catNum = NumberUtil.RandomInt(1, 10);
        }
        else
        {
            try
            {
                if (param1.IsNotNullOrEmpty())
                {
                    catNum = param1.ToInt();
                }
            }
            catch (Exception e)
            {
                await _telegramService.SendTextMessageAsync("Pastikan jumlah kochenk yang diminta berupa angka.");
                return;
            }
        }

        if (catNum > 10)
        {
            await _telegramService.SendTextMessageAsync("Berdasarkan Bot API, batas maksimal Kochenk yg dapat di minta adalah 10.");

            return;
        }

        PrepareKochenk(catNum).InBackground();
    }

    private async Task PrepareKochenk(int catNum)
    {

        await _telegramService.SendTextMessageAsync($"Sedang mempersiapkan {catNum} Kochenk");

        var listAlbum = await _animalsService.GetRandomCatsAwsCat(catNum);

        var sentMessage = await _telegramService.EditMessageTextAsync($"Sedang mengirim {catNum} Kochenk");
        var sendMediaGroup = await _telegramService.SendMediaGroupAsync(listAlbum);

        await _telegramService.DeleteAsync(sentMessage.MessageId);

        if (sendMediaGroup.ErrorException != null)
        {
            var exception = sendMediaGroup.ErrorException;
            await _telegramService.SendTextMessageAsync("Suatu kesalahan terjadi. Silakan dicoba kembali nanti.\n" + exception.Message);
        }
    }
}