using System;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Services.Extensions;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Core;

public class OutCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public OutCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var chatId = _telegramService.ChatId;
        var partsMsg = _telegramService.MessageTextParts;
        var client = _telegramService.Client;

        await _telegramService.DeleteSenderMessageAsync();

        if (!_telegramService.IsFromSudo) return;

        var sendText = "Maaf, saya harus keluar";

        if (partsMsg.ElementAtOrDefault(2) != null)
        {
            sendText += $"\n{partsMsg.ElementAtOrDefault(2)}";
        }

        var targetChatId = partsMsg.ElementAtOrDefault(1).ToInt64();
        Log.Information("Target out: {ChatId}", targetChatId);

        var me = await _telegramService.GetMeAsync();
        var meFullName = me.GetFullName();

        try
        {
            if (targetChatId == 0) targetChatId = chatId;

            await _telegramService.SendTextMessageAsync(
                sendText,
                customChatId: targetChatId,
                replyToMsgId: 0
            );

            await client.LeaveChatAsync(targetChatId);

            if (targetChatId != chatId)
            {
                await _telegramService.SendMessageTextAsync(
                    new MessageResponseDto()
                    {
                        MessageText = $"{meFullName} berhasil keluar dari group" +
                                      $"\nChatId: {targetChatId}",
                        ScheduleDeleteAt = DateTime.UtcNow.AddMinutes(5)
                    }
                );
            }
        }
        catch (Exception e)
        {
            await _telegramService.SendMessageTextAsync(
                new MessageResponseDto()
                {
                    MessageText = $"Sepertinya {meFullName} bukan lagi anggota ChatId {targetChatId}" +
                                  $"\nMessage: {e.Message}",
                    ScheduleDeleteAt = DateTime.UtcNow.AddMinutes(5)
                }
            );
        }
    }
}