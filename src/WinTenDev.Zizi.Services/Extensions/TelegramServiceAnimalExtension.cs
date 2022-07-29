using System;
using System.Linq;
using System.Threading.Tasks;

namespace WinTenDev.Zizi.Services.Extensions;

public static class TelegramServiceAnimalExtension
{
    public static async Task SendRandomCatsAsync(this TelegramService telegramService)
    {
        var catSource = telegramService.CommonConfig.RandomCatSource;
        var message = telegramService.Message;
        var partsText = message.Text.SplitText(" ").ToArray();
        var catNum = 1;
        var param1 = partsText.ElementAtOrDefault(1);

        if (telegramService.IsCommand("/cats"))
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
                await telegramService.SendTextMessageAsync("Pastikan jumlah kochenk yang diminta berupa angka.");
                return;
            }
        }

        if (catNum > 10)
        {
            await telegramService.SendTextMessageAsync("Berdasarkan Bot API, batas maksimal Kochenk yg dapat di minta adalah 10.");

            return;
        }

        try
        {
            await telegramService.SendTextMessageAsync($"Sedang mempersiapkan {catNum} Kochenk");

            var listAlbum = catSource switch
            {
                RandomCatSource.AwsRandomCat => await telegramService.AnimalsService.GetRandomCatsCatApi(catNum),
                RandomCatSource.TheCatApi => await telegramService.AnimalsService.GetRandomCatsCatApi(catNum),
                _ => throw new InvalidCatSourceException("Sumber kochenk tidak diketahui")
            };

            var sentMessage = await telegramService.EditMessageTextAsync($"Sedang mengirim {catNum} Kochenk");
            var sendMediaGroup = await telegramService.SendMediaGroupAsync(listAlbum);

            await telegramService.DeleteAsync(sentMessage.MessageId);

            if (sendMediaGroup.ErrorException != null)
            {
                var exception = sendMediaGroup.ErrorException;
                await telegramService.SendTextMessageAsync("Suatu kesalahan terjadi. Silakan dicoba kembali nanti.\n" + exception.Message);
            }
        }
        catch (Exception exception)
        {
            await telegramService.SendTextMessageAsync(
                sendText: "Suatu kesalahan terjadi. Silakan dicoba kembali nanti.\n" + exception.Message,
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(3)
            );
        }
    }
}