using System;
using System.Linq;
using System.Threading.Tasks;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Telegram.Extensions;

public static class TelegramServiceMemberExtension
{
    public static async Task RestrictMemberAsync(this TelegramService telegramService)
    {
        string muteAnswer;
        var command = telegramService.GetCommand(withoutSlash: true);
        var textParts = telegramService.MessageTextParts.Skip(1);
        var duration = textParts.ElementAtOrDefault(0);

        if (!await telegramService.CheckFromAdminOrAnonymous())
        {
            await telegramService.DeleteSenderMessageAsync();
            return;
        }

        if (telegramService.ReplyToMessage == null)
        {
            muteAnswer = "Silakan reply pesan yang ingin di mute";
        }
        else if (duration == null)
        {
            muteAnswer = "Mau di Mute berapa lama?";
        }
        else
        {
            var muteDuration = duration.ToTimeSpan();

            if (muteDuration < TimeSpan.FromSeconds(30))
            {
                muteAnswer = $"Durasi Mute minimal adalah 30 detik.\nContoh: <code>/mute 30s</code>";
            }
            else
            {
                var isUnMute = command == "unmute";
                var replyFrom = telegramService.ReplyToMessage.From;
                var fromNameLink = replyFrom.GetNameLink();
                var userId = telegramService.ReplyToMessage.From!.Id;

                var muteUntil = muteDuration.ToDateTime();

                var result = await telegramService.RestrictMemberAsync(
                    userId,
                    isUnMute,
                    muteUntil
                );

                if (result.IsSuccess)
                {
                    if (muteDuration > TimeSpan.FromDays(366))
                    {
                        muteAnswer = $"{fromNameLink} telah di mute Selamanya!";
                    }
                    else
                    {
                        muteAnswer = $"{fromNameLink} berhasil di mute." +
                                     $"\nMute berakhir sampai dengan {muteUntil}";
                    }
                }
                else
                {
                    muteAnswer = $"Gagal ketika mengMute {fromNameLink}" +
                                 $"\n{result.Exception.Message}";
                }
            }
        }

        await telegramService.SendTextMessageAsync(
            muteAnswer,
            scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
            includeSenderMessage: true
        );
    }
}