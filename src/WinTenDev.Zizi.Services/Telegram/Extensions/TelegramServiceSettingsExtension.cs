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
    public static async Task SaveInlineSettingsAsync(this TelegramService telegramService)
    {
        if (!await telegramService.CheckUserPermission())
        {
            await telegramService.SendTextMessageAsync("Kamu tidak mempunyai hak akses", scheduleDeleteAt: DateTime.UtcNow.AddSeconds(5));
            return;
        }

        var chatId = telegramService.ChatId;
        var command = telegramService.GetCommand();
        var setKey = command.RemoveThisString(
            "/set_",
            "/set"
        );

        var setValues = telegramService.MessageTextParts
            .Where(s => !s.Contains(telegramService.GetCommand()))
            .ToList();

        if (setValues.Count != 2)
        {
            await telegramService.SendMessageTextAsync(
                new MessageResponseDto()
                {
                    MessageText = "Silakan masukan Key dan Value yang dinginkan",
                    ScheduleDeleteAt = DateTime.UtcNow.AddMinutes(10)
                }
            );

            return;
        }

        var cmdKey = setValues.ElementAtOrDefault(0);
        var cmdValue = setValues.ElementAtOrDefault(1);

        try
        {
            var (verifyKey, verifyValue) = VerifyInlineSettings(cmdKey, cmdValue);

            if (verifyKey == null ||
                verifyValue == null)
            {
                await telegramService.SendMessageTextAsync(
                    new MessageResponseDto()
                    {
                        MessageText = "Key dan Value yang anda masukan tidak valid",
                        ScheduleDeleteAt = DateTime.UtcNow.AddMinutes(10)
                    }
                );

                return;
            }

            await telegramService.SettingsService.UpdateCell(
                chatId: chatId,
                key: verifyKey,
                value: verifyValue
            );

            await telegramService.SendMessageTextAsync(
                new MessageResponseDto()
                {
                    MessageText = "Pengaturan berhasil di perbarui" +
                                  $"\n<code>{verifyKey}</code> => <code>{verifyValue}</code>",
                    ScheduleDeleteAt = DateTime.UtcNow.AddMinutes(1)
                }
            );
        }
        catch (Exception ex)
        {
            await telegramService.SendMessageTextAsync(
                new MessageResponseDto()
                {
                    MessageText = "Terjadi kesalahan saat menyimpan pengaturan" +
                                  $"\n{ex.Message}",
                    ScheduleDeleteAt = DateTime.UtcNow.AddMinutes(10)
                }
            );
        }
    }

    public static (string resultKey, string resultValue) VerifyInlineSettings(
        string key,
        string value
    )
    {
        var resultKey = key switch
        {
            "tz" => "timezone_offset",
            _ => null
        };

        var resultValue = resultKey switch
        {
            "timezone_offset" => TimeUtil.FindTimeZoneByOffsetBase(value)?.BaseUtcOffset.ToStringFormat(@"hh\:mm"),
            _ => null
        };

        return (resultKey, resultValue);
    }

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