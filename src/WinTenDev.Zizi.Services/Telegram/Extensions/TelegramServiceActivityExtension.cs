using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using SerilogTimings;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Telegram.Extensions;

public static class TelegramServiceActivityExtension
{
    public static async Task<bool> OnUpdatePreTaskAsync(this TelegramService telegramService)
    {
        var op = Operation.Begin("Run PreTask for ChatId: {ChatId}", telegramService.ChatId);

        if (telegramService.IsUpdateTooOld()) return false;

        var floodCheck = await telegramService.FloodCheckAsync();
        if (floodCheck.IsFlood)
            return false;

        var hasRestricted = await telegramService.CheckChatRestriction();

        if (hasRestricted)
        {
            return false;
        }

        await telegramService.FireAnalyzer();

        var shouldDelete = await telegramService.ScanMessageAsync();

        var hasSpam = await telegramService.AntiSpamCheckAsync();

        if (hasSpam.IsAnyBanned)
        {
            return false;
        }

        var hasUsername = await telegramService.RunCheckUserUsername();

        if (!hasUsername)
        {
            return false;
        }

        var hasPhotoProfile = await telegramService.RunCheckUserProfilePhoto();

        if (!hasPhotoProfile)
        {
            return false;
        }

        if (shouldDelete)
        {
            return false;
        }

        op.Complete();

        return true;
    }

    public static Task OnUpdatePostTaskAsync(this TelegramService telegramService)
    {
        var op = Operation.Begin("Run PostTask");

        var nonAwaitTasks = new List<Task>
        {
            telegramService.EnsureChatSettingsAsync(),
            telegramService.AfkCheckAsync(),
            telegramService.CheckNameChangesAsync()
        };

        nonAwaitTasks.InBackgroundAll();

        op.Complete();

        return Task.CompletedTask;
    }

    public static async Task AfkCheckAsync(this TelegramService telegramService)
    {
        var operation = Operation.Begin("AFK Check");

        var chatId = telegramService.ChatId;
        var fromId = telegramService.FromId;
        var chatSetting = await telegramService.GetChatSetting();

        try
        {
            Log.Information("Starting check AFK");

            var message = telegramService.MessageOrEdited;

            if (!chatSetting.EnableAfkStatus)
            {
                Log.Information("Afk Stat is disabled in this Group!");
                return;
            }

            if (telegramService.MessageOrEdited == null) return;

            if (message.Text != null &&
                message.Text.StartsWith("/afk")) return;

            if (message.ReplyToMessage != null)
            {
                var repMsg = message.ReplyToMessage;
                var repFromId = repMsg.From.Id;

                var isAfkReply = await telegramService.AfkService.GetAfkById(repFromId);

                if (isAfkReply?.IsAfk ?? false)
                {
                    var repNameLink = repMsg.GetFromNameLink();
                    await telegramService.SendTextMessageAsync(
                        sendText: $"{repNameLink} sedang afk",
                        scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
                        messageFlag: MessageFlag.Afk
                    );

                    telegramService.ChatService
                        .DeleteMessageHistory(
                            history =>
                                history.MessageFlag == MessageFlag.Afk &&
                                history.ChatId == chatId
                        )
                        .InBackground();
                }
                else
                {
                    Log.Debug("No AFK data for '{FromId}' because never recorded as AFK", repFromId);
                }
            }

            var fromAfk = await telegramService.AfkService.GetAfkById(fromId);

            if (fromAfk == null)
            {
                Log.Debug("No AFK data for '{FromId}' because never recorded as AFK", fromId);
                return;
            }

            if (fromAfk.IsAfk)
            {
                var nameLink = message.GetFromNameLink();

                if (fromAfk.IsAfk) await telegramService.SendTextMessageAsync($"{nameLink} sudah tidak afk");

                var data = new Dictionary<string, object>
                {
                    { "chat_id", chatId },
                    { "user_id", fromId },
                    { "is_afk", 0 },
                    { "afk_reason", "" },
                    { "afk_end", DateTime.Now }
                };

                await telegramService.AfkService.SaveAsync(data);
                await telegramService.AfkService.UpdateAfkByIdCacheAsync(fromId);
            }
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "AFK Check - Error occured on {ChatId}",
                chatId
            );
        }

        operation.Complete();
    }
}