using System;
using System.Threading.Tasks;
using Serilog;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Telegram.Extensions;

public static class TelegramServiceHealthyExtension
{
    public static async Task<FloodCheckResult> FloodCheckAsync(this TelegramService telegramService)
    {
        var channelOrEditedPost = telegramService.ChannelOrEditedPost;
        var from = telegramService.From;
        var chat = telegramService.Chat;
        var update = telegramService.Update;
        var messageDate = telegramService.MessageDate;
        var defaultFloodCheck = new FloodCheckResult();

        if (channelOrEditedPost != null) return defaultFloodCheck;

        var chatSettings = await telegramService.GetChatSetting();

        if (!chatSettings.EnableFloodCheck)
        {
            Log.Debug("Flood check disabled for ChatId {ChatId}", chat.Id);
            return defaultFloodCheck;
        }

        var floodCheckResult = telegramService.FloodCheckService.RunFloodCheck
        (
            new HitActivity()
            {
                Guid = Guid.NewGuid().ToString(),
                MessageDate = messageDate,
                UpdateType = update.Type,
                ChatId = chat.Id,
                ChatTitle = chat.Title,
                ChatUsername = chat.Username,
                ChatType = chat.Type,
                FromId = from.Id,
                FromUsername = from.Username,
                FromLangCode = from.LanguageCode,
                FromFirstName = from.FirstName,
                FromLastName = from.LastName,
                Timestamp = DateTime.Now
            }
        );

        if (!floodCheckResult.IsFlood) return floodCheckResult;

        if (!telegramService.IsGroupChat ||
            telegramService.CheckSenderChannel() ||
            await telegramService.CheckFromAdmin())
        {
            return defaultFloodCheck;
        }

        var muteUntilDate = TimeSpan.FromMinutes(floodCheckResult.FloodRate * 5.33).ToDateTime();
        var muteUntilStr = muteUntilDate.ToDetailDateTimeString();

        await telegramService.RestrictMemberAsync(from.Id, until: muteUntilDate);

        var nameLink = from.GetNameLink();

        var text = $"Hai {nameLink}, sepertinya Anda melakukan Flooding!" +
                   $"\nAnda di mute hingga {muteUntilStr} di Obrolan ini";

        await telegramService.SendTextMessageAsync(
            text,
            replyToMsgId: 0,
            scheduleDeleteAt: muteUntilDate.ToUniversalTime(),
            messageFlag: MessageFlag.FloodWarn
        );

        telegramService.ChatService
            .DeleteMessageHistory(
                history =>
                    history.MessageFlag == MessageFlag.FloodWarn &&
                    history.ChatId == chat.Id
            )
            .InBackground();

        return floodCheckResult;
    }
}