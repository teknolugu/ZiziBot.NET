using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using SerilogTimings;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Telegram.Extensions;

public static class TelegramServiceActivityExtension
{
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
                    await telegramService.SendTextMessageAsync($"{repNameLink} sedang afk");
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