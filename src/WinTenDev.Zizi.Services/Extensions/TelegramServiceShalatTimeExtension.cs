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

            if (!await telegramService.CheckUserPermission())
            {
                await telegramService.SendTextMessageAsync(
                    "Untuk waktu Shalat, hanya Admin yang dapat mengatur Kota untuk obrolan ini, " +
                    "namun kamu bisa mengaturnya di Japri untukmu sendiri.",
                    scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
                    includeSenderMessage: true
                );
                return;
            }

            if (inputCity.IsNullOrEmpty())
            {
                await telegramService.SendTextMessageAsync(
                    "Silahkan tulis nama kota yang ingin dicari",
                    scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
                    includeSenderMessage: true
                );
                return;
            }

            var fathimahApiService = telegramService.GetRequiredService<FathimahApiService>();

            var cities = await fathimahApiService.GetAllCityAsync();
            var filterCity = cities.Kota
                .Where(kota => kota.Nama.Contains(inputCity, StringComparison.CurrentCultureIgnoreCase))
                .ToList();

            if (filterCity.Count != 1)
            {
                await telegramService.SendTextMessageAsync(
                    "Ketikkan nama kota yang lebih spesifik",
                    scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
                    includeSenderMessage: true
                );
                return;
            }

            var shalatTome = telegramService.GetRequiredService<ShalatTimeService>();
            var shalatTimeNotifyService = telegramService.GetRequiredService<ShalatTimeNotifyService>();
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

            shalatTimeNotifyService.RegisterJobShalatTime(chatId);

            await telegramService.AppendTextAsync(
                "Kota berhasil disimpan",
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
                includeSenderMessage: true
            );
        }

        public static async Task DeleteCityAsync(this TelegramService telegramService)
        {
            if (!await telegramService.CheckUserPermission())
            {
                await telegramService.SendTextMessageAsync(
                    "Kamu tidak memiliki akses untuk menghapus tetapan Kota",
                    scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
                    includeSenderMessage: true
                );
                return;
            }

            await telegramService.AppendTextAsync("Sedang menghapus Kota");
            var chatId = telegramService.ChatId;
            var shalatTome = telegramService.GetRequiredService<ShalatTimeService>();
            await shalatTome.DeleteCityAsync(chatId);

            await telegramService.AppendTextAsync("Melepaskan penjadwal notifikasi");
            var shalatTimeNotifyService = telegramService.GetRequiredService<ShalatTimeNotifyService>();
            shalatTimeNotifyService.UnRegisterJobShalatTime(chatId);

            await telegramService.AppendTextAsync(
                "Kota berhasil di hapus",
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
                includeSenderMessage: true
            );
        }

        public static async Task GetShalatTimeAsync(this TelegramService telegramService)
        {
            var shalatTimeService = telegramService.GetRequiredService<ShalatTimeService>();
            var fathimahApiService = telegramService.GetRequiredService<FathimahApiService>();

            var chatId = telegramService.ChatId;
            var shalatTime = await shalatTimeService.GetCityByChatId(chatId);

            if (shalatTime == null)
            {
                await telegramService.SendTextMessageAsync(
                    "Kota belum diatur. \nSilakan gunakan <code>/set_city</code> untuk mengatur.",
                    scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
                    includeSenderMessage: true
                );

                return;
            }

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
