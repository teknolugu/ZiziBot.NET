using System;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using Serilog;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Enums.Languages;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.IO;
using WinTenDev.Zizi.Utils.Telegram;

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

        DirUtil.CleanCacheFiles(
            path =>
                path.Contains(chatId.ReduceChatId().ToString()) &&
                path.Contains("ocr")
        );
    }

    public static async Task CreateQrAsync(this TelegramService telegramService)
    {
        var message = telegramService.MessageOrEdited;

        if (message.ReplyToMessage != null)
        {
            message = message.ReplyToMessage;
        }

        var cloneText = message.Text.GetTextWithoutCmd();

        if (cloneText.IsNullOrEmpty())
        {
            var sendTextTr = await telegramService.GetLocalization(Qr.MissingTextOrEmpty);

            await telegramService.SendTextMessageAsync(
                sendText: sendTextTr,
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
                includeSenderMessage: true,
                preventDuplicateSend: true
            );
            return;
        }

        InlineKeyboardMarkup keyboard = null;
        if (telegramService.ReplyToMessage != null)
        {
            var replyToMessage = telegramService.ReplyToMessage;

            var btnCaptionTr = await telegramService.GetLocalization(Qr.SourceButtonCaption);

            keyboard = new InlineKeyboardMarkup(
                InlineKeyboardButton.WithUrl(btnCaptionTr, replyToMessage.GetMessageLink())
            );
        }

        var urlQr = cloneText.GenerateUrlQrApi();
        await telegramService.SendMediaAsync(
            fileId: urlQr.ToString(),
            MediaType.Photo,
            replyMarkup: keyboard,
            scheduleDeleteAt: DateTime.UtcNow.AddMinutes(20),
            includeSenderMessage: true
        );
    }

    public static async Task ReadQrAsync(this TelegramService telegramService)
    {
        try
        {
            var chatId = telegramService.ReducedChatId;

            if (telegramService.ReplyToMessage == null)
            {
                await telegramService.SendTextMessageAsync(
                    sendText: "Silakan balas pesan untuk membaca QR Code",
                    scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
                    includeSenderMessage: true,
                    preventDuplicateSend: true
                );

                return;
            }

            await telegramService.SendTextMessageAsync("Sedang mengambil berkas");

            var localFile = await telegramService.DownloadFileAsync("qr");

            await telegramService.EditMessageTextAsync("Sedang membaca QR Code");
            var qrReadResults = await localFile.ReadQrCodeAsync();

            var symbol = qrReadResults.FirstOrDefault()?.Symbol.FirstOrDefault();
            var result = symbol?.Data ?? "Tidak terdeteksi adanya QR Code";

            await telegramService.EditMessageTextAsync(
                sendText: result,
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(10),
                includeSenderMessage: true
            );

            DirUtil.CleanCacheFiles(
                path =>
                    path.Contains(chatId.ToString()) &&
                    path.Contains("qr")
            );
        }
        catch (FlurlHttpException httpException)
        {
            await telegramService.EditMessageTextAsync(
                "Terjadi kesalahan ketika membaca QR." +
                "\nErrorCode: " +
                httpException.StatusCode
            );
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Error while read qr");
        }
    }

    public static async Task NsfwCheckAsync(this TelegramService telegramService)
    {
        var message = telegramService.Message;

        var replyToMessage = message.ReplyToMessage;

        if (replyToMessage is not { Type: MessageType.Photo })
        {
            await telegramService.SendTextMessageAsync("Silakan balas pesan Gambar yang mau dicek");

            return;
        }

        await telegramService.SendTextMessageAsync("Sedang memeriksa Nudity..");

        var fileName = await telegramService.DownloadFileAsync("nsfw-check");

        var result = await telegramService.DeepAiService.NsfwDetectCoreAsync(fileName);
        var output = result.Output;

        var text = $"NSFW Score: {output.NsfwScore}" +
                   $"\n\nPowered by https://deepai.org";

        await telegramService.EditMessageTextAsync(text, disableWebPreview: true);

        DirUtil.CleanCacheFiles(
            path =>
                path.Contains("nsfw-check") &&
                path.Contains(telegramService.ReducedChatId.ToString())
        );
    }
}