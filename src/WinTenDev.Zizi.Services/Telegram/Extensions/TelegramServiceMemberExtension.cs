using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Telegram.Extensions;

public static class TelegramServiceMemberExtension
{
    public static async Task<AntiSpamResult> AntiSpamCheckAsync(this TelegramService telegramService)
    {
        var fromId = telegramService.FromId;
        var chatId = telegramService.ChatId;

        if (telegramService.IsPrivateChat ||
            telegramService.CheckFromAnonymous() ||
            telegramService.CheckSenderChannel())
        {
            return new AntiSpamResult
            {
                UserId = fromId,
                MessageResult = string.Empty,
                IsAnyBanned = false,
                IsEs2Banned = false,
                IsCasBanned = false,
                IsSpamWatched = false,
            };
        }

        var antiSpamResult = await telegramService.AntiSpamService.CheckSpam(chatId, fromId);

        if (antiSpamResult == null) return null;

        var messageBan = antiSpamResult.MessageResult;

        if (antiSpamResult.IsAnyBanned) await telegramService.KickMemberAsync(fromId, true);

        await telegramService.SendTextMessageAsync(messageBan, replyToMsgId: 0);

        return antiSpamResult;
    }

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

    public static async Task CheckNameChangesAsync(this TelegramService telegramService)
    {
        var fromId = telegramService.FromId;
        var chatId = telegramService.ChatId;

        try
        {
            if (telegramService.MessageOrEdited == null) return;

            var updateType = telegramService.Update.Type;
            var currentChat = telegramService.Chat;
            var fromUsername = telegramService.From.Username;
            var fromFirstName = telegramService.From.FirstName;
            var fromLastName = telegramService.From.LastName;
            var fromLanguageCode = telegramService.From.LanguageCode;

            var chatSettings = await telegramService.GetChatSetting();
            if (!chatSettings.EnableZiziMata)
            {
                Log.Information("MataZizi is disabled in this Group!");
                return;
            }

            var botUser = await telegramService.GetMeAsync();

            var userData = new HitActivity
            {
                ViaBot = botUser.Username,
                UpdateType = updateType,
                FromId = fromId,
                FromFirstName = fromFirstName,
                FromLastName = fromLastName,
                FromUsername = fromUsername,
                FromLangCode = fromLanguageCode,
                ChatId = chatId,
                ChatUsername = currentChat.Username,
                ChatType = currentChat.Type,
                ChatTitle = currentChat.Title,
                Timestamp = DateTime.UtcNow
            };

            Log.Information("Starting SangMata check..");

            var hitActivityCache = await telegramService.MataService.GetMataCore(fromId);
            if (hitActivityCache.IsNull)
            {
                Log.Information("This may first Hit from User {UserId}", fromId);

                await telegramService.MataService.SaveMataAsync(fromId, userData);

                return;
            }

            var changesCount = 0;
            var msgBuild = new StringBuilder();

            msgBuild.AppendLine("😽 <b>MataZizi</b>");
            msgBuild.Append("<b>UserID:</b> ").Append(fromId).AppendLine();

            var hitActivity = hitActivityCache.Value;

            if (fromUsername != hitActivity.FromUsername)
            {
                Log.Debug("Username changed detected!");
                if (fromUsername.IsNullOrEmpty())
                    msgBuild.AppendLine("Menghapus Usernamenya");
                else
                    msgBuild.Append("Mengubah Username menjadi @").AppendLine(fromUsername);

                changesCount++;
            }

            if (fromFirstName != hitActivity.FromFirstName)
            {
                Log.Debug("First Name changed detected!");
                if (fromFirstName.IsNullOrEmpty())
                    msgBuild.AppendLine("Menghapus nama depannya.");
                else
                    msgBuild.Append("Mengubah nama depan menjadi ").AppendLine(fromFirstName);

                changesCount++;
            }

            if (fromLastName != hitActivity.FromLastName)
            {
                Log.Debug("Last Name changed detected!");
                if (fromLastName.IsNullOrEmpty())
                    msgBuild.AppendLine("Menghapus nama belakangnya");
                else
                    msgBuild.Append("Mengubah nama belakang menjadi ").AppendLine(fromLastName);

                changesCount++;
            }

            if (changesCount > 0)
            {
                await telegramService.SendTextMessageAsync(
                    sendText: msgBuild.ToString().Trim(),
                    scheduleDeleteAt: DateTime.UtcNow.AddMinutes(10)
                );

                await telegramService.MataService.SaveMataAsync(fromId, userData);

                Log.Debug("Complete update Cache");
            }

            Log.Information("MataZizi completed . Changes: {ChangesCount}", changesCount);
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "Error SangMata at ChatId {ChatId}",
                chatId
            );
        }
    }
}