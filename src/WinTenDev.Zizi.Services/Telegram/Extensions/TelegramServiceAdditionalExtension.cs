using System;
using System.Threading.Tasks;
using Flurl.Http;
using Serilog;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.IO;

namespace WinTenDev.Zizi.Services.Telegram.Extensions;

public static class TelegramServiceAdditionalExtension
{
    public static async Task OptiicDevOcrAsync(this TelegramService telegramService)
    {
        var msg = telegramService.Message;
        var chatId = telegramService.ChatId;

        if (msg.ReplyToMessage == null)
        {
            await telegramService.SendTextMessageAsync("Silakan reply salah satu gambar");
            return;
        }

        var repMsg = msg.ReplyToMessage;

        if (repMsg.Photo == null)
        {
            await telegramService.SendTextMessageAsync("Silakan balas pesan yang berisi gambar");
            return;
        }

        await telegramService.SendTextMessageAsync("Sedang memproses gambar");
        var savedFile = await telegramService.DownloadFileAsync("ocr");

        try
        {
            Log.Information("Preparing send file to Optiic OCR");

            var optiicDevOcr = await telegramService.OptiicDevService.ScanImageText(savedFile);
            var ocr = optiicDevOcr.Text;

            if (ocr.IsNullOrEmpty())
            {
                ocr = "Tidak terdeteksi adanya teks di gambar tersebut";
            }

            await telegramService.EditMessageTextAsync(ocr);
        }
        catch (FlurlHttpException exception)
        {
            await telegramService.EditMessageTextAsync(
                "Terjadi kesalahan saat memproses gambar." +
                $"\nError Kode: {exception.StatusCode}"
            );
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Error while OCR");
        }

        savedFile.GetDirectory()
            .RemoveFiles(
                path =>
                    path.Contains(chatId.ToString()) &&
                    path.Contains("ocr")
            );
    }
}