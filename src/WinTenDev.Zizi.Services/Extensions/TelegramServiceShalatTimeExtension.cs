using System;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace WinTenDev.Zizi.Services.Extensions
{
    public static class TelegramServiceShalatTimeExtension
    {
        public static async Task SaveShalatTimeCityAsync(this TelegramService telegramService)
        {
            var chatId = telegramService.ChatId;
            var userId = telegramService.FromId;

            var inputCity = telegramService.MessageOrEditedText.GetTextWithoutCmd();

            if (!await telegramService.CheckUserPermission())
            {
                await telegramService.SendTextMessageAsync(
                    sendText: "Untuk waktu Shalat, hanya Admin yang dapat mengatur Kota untuk obrolan ini, " +
                              "namun kamu bisa mengaturnya di Japri untukmu sendiri.",
                    scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
                    includeSenderMessage: true
                );
                return;
            }

            if (inputCity.IsNullOrEmpty())
            {
                await telegramService.SendTextMessageAsync(
                    sendText: "Silahkan tulis nama kota yang ingin dicari",
                    scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
                    includeSenderMessage: true
                );
                return;
            }

            var fathimahApiService = telegramService.GetRequiredService<FathimahApiService>();

            var cities = await fathimahApiService.GetAllCityAsync();
            var filteredCity = cities.Kota
                .Where(
                    kota =>
                        kota.Nama.Contains(inputCity, StringComparison.CurrentCultureIgnoreCase) ||
                        kota.Id.ToString() == inputCity
                )
                .ToList();
            var filteredCityCount = filteredCity.Count;
            var filteredCityStr = filteredCity
                .Select(
                    (
                        kota,
                        index
                    ) => $"{index + 1}. <code>{kota.Id}</code> <code>{kota.Nama}</code>"
                )
                .JoinStr("\n");

            var findResult = filteredCity.Count switch
            {
                0 => "Kota yang di masukkan tidak di temukan, silakan cari kota lain",
                > 1 => $"Ditemukan sebanyak {filteredCityCount} kota, silakan pilih salah satu" +
                       $"\n{filteredCityStr}",
                _ => null
            };

            if (findResult != null)
            {
                await telegramService.SendTextMessageAsync(
                    sendText: findResult,
                    scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
                    includeSenderMessage: true
                );

                return;
            }

            var shalatTimeService = telegramService.GetRequiredService<ShalatTimeService>();
            var shalatTimeNotifyService = telegramService.GetRequiredService<ShalatTimeNotifyService>();
            var firstCity = filteredCity.FirstOrDefault();

            await telegramService.AppendTextAsync($"<b>Kota/Kab ID: </b><code>{firstCity.Id}</code>");
            await telegramService.AppendTextAsync($"<b>Nama: </b><code>{firstCity.Nama}</code>");

            if (await shalatTimeService.IsExistAsync(chatId, firstCity.Nama))
            {
                await telegramService.AppendTextAsync(
                    sendText: "Sepertinya kota ini sudah ditambahkan",
                    scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5)
                );
                return;
            }

            await telegramService.AppendTextAsync("Sedang menyimpan data kota");
            await shalatTimeService.SaveCityAsync(
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
                sendText: "Kota berhasil disimpan",
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
                includeSenderMessage: true
            );
        }

        public static async Task DeleteCityAsync(this TelegramService telegramService)
        {
            if (!await telegramService.CheckUserPermission())
            {
                await telegramService.SendTextMessageAsync(
                    sendText: "Kamu tidak memiliki akses untuk menghapus tetapan Kota",
                    scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
                    includeSenderMessage: true
                );
                return;
            }

            var cityName = telegramService.MessageOrEditedText.GetTextWithoutCmd();
            if (cityName.IsNullOrEmpty())
            {
                await telegramService.SendTextMessageAsync(
                    sendText: "Kota apa yang mau di hapus?",
                    scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
                    includeSenderMessage: true
                );
                return;
            }

            await telegramService.AppendTextAsync("Sedang menghapus Kota");
            var chatId = telegramService.ChatId;
            var shalatTome = telegramService.GetRequiredService<ShalatTimeService>();
            var deleteItem = await shalatTome.DeleteCityAsync(chatId, cityName);

            var deleteResult = deleteItem switch
            {
                > 0 => "Kota berhasil dihapus",
                0 => "Sepetinya kota tidak ditemukan",
                _ => null,
            };

            await telegramService.AppendTextAsync(
                sendText: deleteResult,
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
                includeSenderMessage: true
            );
        }

        public static async Task GetShalatTimeAsync(this TelegramService telegramService)
        {
            var shalatTimeService = telegramService.GetRequiredService<ShalatTimeService>();
            var fathimahApiService = telegramService.GetRequiredService<FathimahApiService>();

            var chatId = telegramService.ChatId;
            var listCities = await shalatTimeService.GetCities(chatId);
            var listCitiesCount = listCities.Count;

            Log.Debug(
                "Got {Count} cities for ChatId: {ChatId}",
                listCitiesCount,
                chatId
            );

            if (listCitiesCount == 0)
            {
                await telegramService.SendTextMessageAsync(
                    sendText: "Kota belum diatur. \nSilakan gunakan <code>/add_city</code> untuk menambahkan.",
                    scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
                    includeSenderMessage: true
                );

                return;
            }

            var time = HtmlMessage.Empty;
            time.BoldBr($"⏳ Waktu Shalat ({listCitiesCount} Kota)")
                .Br();

            foreach (var shalatTime in listCities)
            {
                var shalatTimeResponse = await fathimahApiService.GetShalatTime(
                    dateTime: DateTime.Now,
                    cityId: shalatTime.CityId
                );

                var jadwalStr = shalatTimeResponse.Jadwal.Data;
                time.Bold("Kota/Kab: ").CodeBr(shalatTime.CityName)
                    .Bold("Tanggal: ").CodeBr(jadwalStr.Tanggal)
                    .Bold("Dzuhur ").CodeBr(jadwalStr.Dzuhur)
                    .Bold("Ashar ").CodeBr(jadwalStr.Ashar)
                    .Bold("Maghrib ").CodeBr(jadwalStr.Maghrib)
                    .Bold("Isya ").CodeBr(jadwalStr.Isya)
                    .Bold("Imsak ").CodeBr(jadwalStr.Imsak)
                    .Bold("Subuh ").CodeBr(jadwalStr.Subuh)
                    .Bold("Terbit ").CodeBr(jadwalStr.Terbit)
                    .Bold("Dhuha ").CodeBr(jadwalStr.Dhuha)
                    .Br();
            }

            await telegramService.AppendTextAsync(
                sendText: time.ToString(),
                scheduleDeleteAt: DateTime.UtcNow.AddDays(1),
                includeSenderMessage: true
            );
        }

        public static async Task GetCityListAsync(this TelegramService telegramService)
        {
            var chatId = telegramService.ChatId;
            var shalatTimeService = telegramService.GetRequiredService<ShalatTimeService>();
            var listCities = await shalatTimeService.GetCities(chatId);

            var listCitiesMessage = HtmlMessage.Empty
                .BoldBr("📍 Daftar Kota di Obrolan ini")
                .TextBr(
                    listCities
                        .Select(
                            (
                                shalatTime,
                                index
                            ) => $"{index + 1}. <code>{shalatTime.CityName.ToTitleCase()}</code>"
                        ).JoinStr("\n")
                );

            await telegramService.AppendTextAsync(
                sendText: listCitiesMessage.ToString(),
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
                includeSenderMessage: true
            );
        }
    }
}