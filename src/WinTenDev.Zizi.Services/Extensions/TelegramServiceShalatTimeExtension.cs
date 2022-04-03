using System;
using System.Linq;
using System.Threading.Tasks;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Externals;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.Services.Extensions
{
    public static class TelegramServiceShalatTimeExtension
    {
        public static async Task SaveShalatTimeCityAsync(this TelegramService telegramService)
        {
            var chatId = telegramService.ChatId;
            var userId = telegramService.FromId;

            var inputCity = telegramService.GetCommandParam(0);

            if (inputCity.IsNullOrEmpty())
            {
                await telegramService.SendTextMessageAsync("Silahkan tulis nama kota yang ingin dicari");
                return;
            }

            var fathimahApiService = telegramService.GetRequiredService<FathimahApiService>();

            var cities = await fathimahApiService.GetAllCityAsync();
            var filterCity = cities.Kota
                .Where(kota => kota.Nama.Contains(inputCity, StringComparison.CurrentCultureIgnoreCase))
                .ToList();

            if (filterCity.Count != 1)
            {
                await telegramService.SendTextMessageAsync("Ketikkan nama kota yang lebih spesifik");
                return;
            }

            var shalatTome = telegramService.GetRequiredService<ShalatTimeService>();
            var firstCity = filterCity.FirstOrDefault();

            await telegramService.AppendTextAsync($"<b>Kota/Kab ID: </b><code>{firstCity.Id}</code>");
            await telegramService.AppendTextAsync($"<b>Nama: </b><code>{firstCity.Nama}</code>");
            await telegramService.AppendTextAsync("Sedang menyimpan data kota");
            await shalatTome.SaveCityAsync(
                new ShalatTime()
                {
                    ChatId = chatId,
                    UserId = userId,
                    CityId = firstCity.Id,
                    CityName = firstCity.Nama,
                    EnableNotification = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            await telegramService.AppendTextAsync("Kota berhasil disimpan");
        }

        public static async Task GetShalatTimeAsync(this TelegramService telegramService)
        {
            var shalatTimeService = telegramService.GetRequiredService<ShalatTimeService>();
            var fathimahApiService = telegramService.GetRequiredService<FathimahApiService>();

            var chatId = telegramService.ChatId;
            var shalatTime = await shalatTimeService.GetCityByChatId(chatId);

            var shalatTimeResponse = await fathimahApiService.GetShalatTime(
                dateTime: DateTime.Now,
                cityId: shalatTime.CityId
            );

            var jadwalStr = shalatTimeResponse.Jadwal.Data;
            var time = HtmlMessage.Empty
                .BoldBr("⏳ Waktu Shalat")
                .Bold("Kota/Kab: ").CodeBr(shalatTime.CityName)
                .Bold("Tanggal: ").CodeBr(jadwalStr.Tanggal)
                .Bold("Dzuhur ").CodeBr(jadwalStr.Dzuhur)
                .Bold("Ashar ").CodeBr(jadwalStr.Ashar)
                .Bold("Maghrib ").CodeBr(jadwalStr.Maghrib)
                .Bold("Isya ").CodeBr(jadwalStr.Isya)
                .Bold("Subuh ").CodeBr(jadwalStr.Subuh)
                .Bold("Terbit ").CodeBr(jadwalStr.Terbit);

            await telegramService.AppendTextAsync(time.ToString());
        }
    }
}
