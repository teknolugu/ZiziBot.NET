using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Telegram.Extensions;

public static class TelegramServiceSettingsExtension
{
    public static async Task SaveWelcomeSettingsAsync(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;
        var message = telegramService.MessageOrEdited;
        var command = telegramService.GetCommand();
        var key = command.Remove(0, 5);

        var columnTarget = new Dictionary<string, string>()
        {
            { "welcome_btn", "welcome_button" },
            { "welcome_doc", "welcome_document" },
            { "welcome_msg", "welcome_message" },
        }.FirstOrDefault(pair => pair.Key == key).Value;

        var featureName = columnTarget.Titleize();

        await telegramService.SendTextMessageAsync($"Sedang menyimpan {featureName}..");

        if (key == "welcome_doc")
        {
            var mediaFileId = telegramService.ReplyToMessage.GetFileId();
            var mediaType = telegramService.ReplyToMessage.Type;

            await telegramService.SettingsService.UpdateCell(
                chatId: chatId,
                key: "welcome_media",
                value: mediaFileId
            );
            await telegramService.SettingsService.UpdateCell(
                chatId: chatId,
                key: "welcome_media_type",
                value: mediaType
            );
        }
        else
        {
            var welcomeData = message.CloneText().GetTextWithoutCmd();

            if (welcomeData.IsNullOrEmpty())
            {
                await telegramService.EditMessageTextAsync(
                    new MessageResponseDto()
                    {
                        MessageText = $"Silakan tentukan data untuk {featureName} " +
                                      "atau balas sebuah pesan yang diinginkan.",
                        ScheduleDeleteAt = DateTime.UtcNow.AddMinutes(1),
                        IncludeSenderForDelete = true
                    }
                );
                return;
            }

            await telegramService.SettingsService.UpdateCell(
                chatId: chatId,
                key: columnTarget,
                value: welcomeData
            );
        }

        await telegramService.EditMessageTextAsync(
            new MessageResponseDto()
            {
                MessageText = $"<b>{featureName}</b> berhasil di simpan!" +
                              $"\nKetik <code>/welcome</code> untuk melihat perubahan",
                ScheduleDeleteAt = DateTime.UtcNow.AddMinutes(1),
                IncludeSenderForDelete = true
            }
        );
    }
}