using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Humanizer;
using Serilog;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Enums;
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
            await telegramService.CheckFromAdminOrAnonymous() ||
            telegramService.CheckSenderChannel())
        {
            return new AntiSpamResult
            {
                UserId = fromId,
                MessageResult = string.Empty,
                IsAnyBanned = false,
                IsEs2Banned = false,
                IsCasBanned = false,
                IsSpamWatched = false
            };
        }

        var antiSpamResult = await telegramService.AntiSpamService.CheckSpam(chatId, fromId);

        if (antiSpamResult == null) return null;

        var messageBan = antiSpamResult.MessageResult;

        if (!antiSpamResult.IsAnyBanned) return antiSpamResult;

        await Task.WhenAll(
            telegramService.KickMemberAsync(fromId, true),
            telegramService.DeleteSenderMessageAsync(),
            telegramService.SendTextMessageAsync(
                sendText: messageBan,
                replyToMsgId: 0,
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(10),
                preventDuplicateSend: true,
                messageFlag: MessageFlag.GBan
            )
        );

        return antiSpamResult;
    }

    public static async Task RestrictMemberAsync(this TelegramService telegramService)
    {
        try
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
                            var untilDateStr = muteUntil.ToDetailDateTimeString();
                            muteAnswer = $"{fromNameLink} berhasil di mute." +
                                         $"\nMute berakhir sampai dengan {untilDateStr}";
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
        catch (Exception exception)
        {
            await telegramService.SendTextMessageAsync(
                $"Gagal ketika Mute pengguna. " +
                $"\n{exception.Message}"
            );
        }
    }

    public static async Task CheckNameChangesAsync(this TelegramService telegramService)
    {
        var fromId = telegramService.FromId;
        var chatId = telegramService.ChatId;

        try
        {
            if (telegramService.MessageOrEdited == null) return;

            var updateType = telegramService.Update.Type.Humanize().Pascalize();
            var currentChat = telegramService.Chat;
            var fromUsername = telegramService.From.Username;
            var fromFirstName = telegramService.From.FirstName;
            var fromLastName = telegramService.From.LastName;
            var fromLanguageCode = telegramService.From.LanguageCode;

            var chatSettings = await telegramService.GetChatSetting();
            if (!chatSettings.EnableZiziMata)
            {
                Log.Information("MataZizi is disabled at ChatId: {ChatId}", chatId);
                return;
            }

            var botUser = await telegramService.GetMeAsync();

            var userHistory = new UserHistory()
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
                ChatType = currentChat.Type.Humanize().Pascalize(),
                ChatTitle = telegramService.ChatTitle,
                MessageDate = telegramService.MessageDate,
                Timestamp = DateTime.UtcNow
            };

            Log.Information(
                "Starting SangMata check at ChatId: {ChatId} for UserId: {UserId}",
                chatId,
                fromId
            );

            var lastActivity = await telegramService.MataService.GetLastMataAsync(fromId);
            if (lastActivity == null)
            {
                Log.Information(
                    "This may first Hit from UserId {UserId} at ChatId: {ChatId}",
                    fromId,
                    chatId
                );

                await telegramService.MataService.SaveMataAsync(userHistory);

                return;
            }

            var changesCount = 0;
            var msgBuild = new StringBuilder();

            msgBuild.AppendLine("😽 <b>MataZizi</b>");
            msgBuild.Append("<b>UserID:</b> ").Append(fromId).AppendLine();

            if (fromUsername != lastActivity.FromUsername)
            {
                Log.Debug("Username changed detected for UserId: {UserId}", fromId);
                if (fromUsername.IsNullOrEmpty())
                    msgBuild.AppendLine("Menghapus Usernamenya");
                else
                    msgBuild.Append("Mengubah Username menjadi @").AppendLine(fromUsername);

                changesCount++;
            }

            if (fromFirstName != lastActivity.FromFirstName)
            {
                Log.Debug("First Name changed detected for UserId: {UserId}", fromId);
                if (fromFirstName.IsNullOrEmpty())
                    msgBuild.AppendLine("Menghapus nama depannya.");
                else
                    msgBuild.Append("Mengubah nama depan menjadi ").AppendLine(fromFirstName);

                changesCount++;
            }

            if (fromLastName != lastActivity.FromLastName)
            {
                Log.Debug("Last Name changed detected for UserId: {UserId}", fromId);
                if (fromLastName.IsNullOrEmpty())
                    msgBuild.AppendLine("Menghapus nama belakangnya");
                else
                    msgBuild.Append("Mengubah nama belakang menjadi ").AppendLine(fromLastName);

                changesCount++;
            }

            if (changesCount > 0)
            {
                await Task.WhenAll(
                    telegramService.SendTextMessageAsync(
                        sendText: msgBuild.ToString().Trim(),
                        scheduleDeleteAt: DateTime.UtcNow.AddMinutes(10),
                        messageFlag: MessageFlag.ZiziMata
                    ),
                    telegramService.ChatService
                        .DeleteMessageHistory(
                            history =>
                                history.MessageFlag == MessageFlag.ZiziMata &&
                                history.ChatId == chatId
                        ),
                    telegramService.MataService.SaveMataAsync(userHistory)
                );
            }

            Log.Information(
                "MataZizi completed for UserId: {UserId} at ChatId: {ChatId}. Total Changes: {ChangesCount}",
                fromId,
                chatId,
                changesCount
            );
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "Error MataZizi at ChatId: {ChatId} for UserId: {UserId}",
                chatId,
                fromId
            );
        }
    }

    public static async Task<bool> CheckSubscriptionIntoLinkedChannelAsync(this TelegramService telegramService)
    {
        var fromId = telegramService.FromId;
        var chatId = telegramService.ChatId;

        try
        {
            if (await telegramService.CheckUserPermission())
            {
                Log.Information(
                    "UserId: {UserId} at ChatId: {ChatId} is Have privilege for skip Force Subscription",
                    fromId,
                    chatId
                );

                return true;
            }

            var settings = await telegramService.GetChatSetting();
            if (!settings.EnableForceSubscription)
            {
                Log.Information(
                    "Force Subscription is disabled at ChatId: {ChatId}",
                    chatId
                );

                return true;
            }

            var fromNameLink = telegramService.FromNameLink;

            var getChat = await telegramService.GetChat();
            var linkedChatId = getChat.LinkedChatId ?? 0;

            if (getChat.LinkedChatId == null) return true;
            var chatLinked = await telegramService.ChatService.GetChatAsync(linkedChatId);

            if (chatLinked.Username == null)
            {
                Log.Information(
                    "Force Subs for ChatId: {ChatId} is disabled because linked channel with Id: {LinkedChatId} is not a Public Channel",
                    chatId,
                    linkedChatId
                );

                return true;
            }

            var chatMember = await telegramService.ChatService.GetChatMemberAsync(
                chatId: linkedChatId,
                userId: fromId,
                evictAfter: true
            );

            if (chatMember.Status != ChatMemberStatus.Left) return true;

            var keyboard = new InlineKeyboardMarkup(
                InlineKeyboardButton.WithUrl(chatLinked.GetChatTitle(), chatLinked.GetChatLink())
            );
            var sendText = $"Hai {fromNameLink}" + 
                           "\nKamu belum Subscribe ke Channel dibawah ini, silakan segera Subcribe agar tidak di tendang.";

            await telegramService.SendTextMessageAsync(
                sendText: sendText,
                replyMarkup: keyboard,
                replyToMsgId: 0,
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
                messageFlag: MessageFlag.ForceSubscribe,
                preventDuplicateSend: true
            );

            await telegramService.ScheduleKickJob(StepHistoryName.ForceSubscription);
        }
        catch (Exception exception)
        {
            Log.Warning(
                exception,
                "Error When check subscription into linked channel. ChatId: {ChatId}",
                chatId
            );

            return true;
        }

        return false;
    }

    public static async Task AddGlobalBanAsync(this TelegramService telegramService)
    {
        long userId;
        string reason;

        var msg = telegramService.Message;

        var chatId = telegramService.ChatId;
        var fromId = telegramService.FromId;
        var partedText = telegramService.MessageTextParts;
        var param0 = partedText.ElementAtOrDefault(0) ?? "";
        var param1 = partedText.ElementAtOrDefault(1) ?? "";

        await telegramService.DeleteSenderMessageAsync();

        if (!telegramService.IsFromSudo)
        {
            return;
        }

        if (telegramService.ReplyToMessage != null)
        {
            var replyToMessage = telegramService.ReplyToMessage;
            userId = replyToMessage.From.Id;
            reason = msg.Text;

            if (replyToMessage.ForwardFrom != null)
            {
                userId = replyToMessage.ForwardFrom.Id;
            }

            if (reason.IsNotNullOrEmpty())
                reason = partedText.Skip(1).JoinStr(" ").Trim();
        }
        else
        {
            if (param1.IsNullOrEmpty())
            {
                await telegramService.SendTextMessageAsync(
                    sendText: "Balas seseorang yang mau di ban",
                    scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
                    includeSenderMessage: true
                );

                return;
            }

            userId = param1.ToInt64();
            reason = msg.Text;

            if (reason.IsNotNullOrEmpty())
                reason = partedText.Skip(2).JoinStr(" ").Trim();
        }

        Log.Information("Execute Global Ban");
        await telegramService.AppendTextAsync($"<b>Global Ban</b>", replyToMsgId: 0);
        await telegramService.AppendTextAsync($"Telegram UserId: <code>{userId}</code>");

        if (await telegramService.CheckFromAdmin(userId))
        {
            await telegramService.AppendTextAsync($"Tidak dapat melakukan Global Ban kepada Admin");
            return;
        }

        reason = reason.IsNullOrEmpty() ? "General SpamBot" : reason;

        var banData = new GlobalBanItem()
        {
            UserId = userId,
            BannedBy = fromId,
            BannedFrom = chatId,
            ReasonBan = reason
        };

        var isBan = await telegramService.GlobalBanService.IsExist(userId);

        if (isBan)
        {
            await telegramService.AppendTextAsync(
                sendText: "Pengguna sudah di ban",
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(2),
                includeSenderMessage: true
            );

            return;
        }

        await telegramService.AppendTextAsync("Menyimpan informasi..");
        var save = await telegramService.GlobalBanService.SaveBanAsync(banData);

        await telegramService.AppendTextAsync($"Alasan: {reason}");

        Log.Information("SaveBan: {Save}", save);

        await telegramService.AppendTextAsync("Memperbarui cache.");
        await telegramService.GlobalBanService.UpdateCache(userId);

        await telegramService.AppendTextAsync(
            sendText: "Pengguna berhasil di tambahkan",
            scheduleDeleteAt: DateTime.UtcNow.AddMinutes(2),
            includeSenderMessage: true
        );
    }
}