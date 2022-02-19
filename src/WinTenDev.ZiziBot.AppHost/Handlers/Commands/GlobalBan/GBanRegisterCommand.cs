using System;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.GlobalBan;

public class GBanRegisterCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly GlobalBanService _globalBanService;

    public GBanRegisterCommand(
        TelegramService telegramService,
        GlobalBanService globalBanService
    )
    {
        _telegramService = telegramService;
        _globalBanService = globalBanService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var message = _telegramService.Message;
        var userId = message.From.Id;

        if (!await _telegramService.IsBeta()) return;

        await _telegramService.SendTextMessageAsync("Sedang memeriksa persyaratan");

        if (_telegramService.IsPrivateChat)
        {
            await _telegramService.EditMessageTextAsync("Register Fed ES2 tidak dapat dilakukan di Private Chat.");
            return;
        }

        if (!await _telegramService.CheckFromAdmin())
        {
            await _telegramService.EditMessageTextAsync("Hanya admin yang dapat register ke Fed ES2.");
            return;
        }

        var memberCount = await _telegramService.GetMemberCount();
        if (memberCount < 197)
        {
            await _telegramService.EditMessageTextAsync("Jumlah member di Grup ini kurang dari persyaratan minimum.");
            return;
        }

        if (message.ReplyToMessage != null)
        {
            var repMsg = message.ReplyToMessage;
            if (repMsg.From.IsBot)
            {
                await _telegramService.EditMessageTextAsync("Tidak dapat meregister Bot menjadi admin ES2");
                return;
            }

            userId = message.ReplyToMessage.From.Id;
        }

        var adminItem = new GlobalBanAdminItem()
        {
            Username = message.From.Username,
            UserId = userId,
            PromotedBy = message.From.Id,
            PromotedFrom = message.Chat.Id,
            CreatedAt = DateTime.Now,
            IsBanned = false
        };

        var isRegistered = await _globalBanService.IsGBanAdminAsync(userId);
        if (isRegistered)
        {
            await _telegramService.EditMessageTextAsync($"Sepertinya UserID {adminItem.UserId} sudah menjadi Admin Fed");
            return;
        }

        await _telegramService.EditMessageTextAsync("Sedang meregister ke GBan Admin");
        await _globalBanService.SaveAdminBan(adminItem);

        await _telegramService.EditMessageTextAsync("Selesai");
    }
}