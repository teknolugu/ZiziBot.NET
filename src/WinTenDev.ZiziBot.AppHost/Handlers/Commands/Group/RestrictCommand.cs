using System;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Services.Telegram.Extensions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Group;

public class RestrictCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public RestrictCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        try
        {
            await _telegramService.RestrictMemberAsync();
        }
        catch (Exception ex)
        {
            await _telegramService.SendTextMessageAsync(
                $"Gagal ketika Mute pengguna. " +
                $"\n{ex.Message}"
            );
        }
    }
}