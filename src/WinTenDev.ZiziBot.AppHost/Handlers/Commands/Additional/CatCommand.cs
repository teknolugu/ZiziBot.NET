using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using Nito.AsyncEx;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Additional;

public class CatCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private const string CatSource = "https://aws.random.cat/meow";

    public CatCommand(TelegramService telegramService)
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
            await _telegramService.SendTextMessageAsync("Berdasarkan Bot API, Batas maksimal Kochenk yg dapat di minta adalah 10.");

            return;
        }

        PrepareKochenk(catNum).Ignore();
    }

    private async Task PrepareKochenk(int catNum)
    {
        var listAlbum = new List<IAlbumInputMedia>();

        await _telegramService.SendTextMessageAsync($"Sedang mempersiapkan {catNum} Kochenk");

        for (var i = 1; i <= catNum; i++)
        {
            Log.Information("Loading cat {I} of {CatNum} from {CatSource}", i, catNum, CatSource);

            var url = await CatSource.GetJsonAsync<CatMeow>();
            var urlFile = url.File.AbsoluteUri;

            Log.Debug("Adding kochenk {UrlFile}", urlFile);

            var fileName = Path.GetFileName(urlFile);

            listAlbum.Add(new InputMediaPhoto(new InputMedia(urlFile)
            {
                FileName = fileName
            })
            {
                Caption = $"Kochenk {i}"
            });
        }

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