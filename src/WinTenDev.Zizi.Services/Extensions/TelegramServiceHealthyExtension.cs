using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Serilog;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Extensions;

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
            await telegramService.CheckFromAdminOrAnonymous())
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

    [SuppressMessage("ReSharper", "SpecifyACultureInStringConversionExplicitly")]
    public static async Task BotSlowdownNotification(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;

        try
        {
            var eventLogService = telegramService.GetRequiredService<EventLogService>();
            var healthConfig = telegramService.GetRequiredService<IOptionsSnapshot<HealthConfig>>().Value;

            if (telegramService.ChannelOrEditedPost != null) return;
            if (telegramService.CallbackQuery != null) return;

            var timeInit = telegramService.TimeInit.ToDouble();
            var timeProc = telegramService.TimeProc.ToDouble();

            if (timeInit >= healthConfig.SlowdownOffset ||
                timeProc >= healthConfig.SlowdownOffset)
            {
                Log.Information(
                    "Bot slowdown detected. Time Init: {TimeInit}, TimeProc: {TimeProc}",
                    timeInit,
                    timeProc
                );

                var memberCount = await telegramService.GetMemberCount();

                var message = telegramService.Message;

                if (message == null) return;

                var htmlMessage = HtmlMessage.Empty
                    .TextBr("Uh Oh, Saya melambat!")
                    .Bold("Response: ").CodeBr(timeInit.ToString())
                    .Bold("Eksekusi: ").CodeBr(timeProc.ToString())
                    .Bold("Jumlah Anggota: ").CodeBr(memberCount.ToString());

                await eventLogService.SendEventLogAsync(
                    text: htmlMessage.ToString(),
                    message: message,
                    sendGlobalOnly: true,
                    messageFlag: MessageFlag.SlowDown
                );
            }
            else
            {
                Log.Information(
                    "Slowdown Offset not be reached! Time Init: {TimeInit}, TimeProc: {TimeProc}",
                    timeInit,
                    timeProc
                );
            }
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "Error on send slowdown notification from ChatId: {ChatId}",
                chatId
            );
        }
    }
}
