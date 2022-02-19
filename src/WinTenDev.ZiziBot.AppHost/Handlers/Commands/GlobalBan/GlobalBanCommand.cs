using System;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.GlobalBan;

public class GlobalBanCommand : CommandBase
{
    private readonly GlobalBanService _globalBanService;
    private readonly TelegramService _telegramService;

    public GlobalBanCommand(
        GlobalBanService globalBanService,
        TelegramService telegramService
    )
    {
        _globalBanService = globalBanService;
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        long userId;
        string reason;

        var msg = _telegramService.Message;

        var chatId = _telegramService.ChatId;
        var fromId = _telegramService.FromId;
        var partedText = _telegramService.MessageTextParts;
        var param0 = partedText.ElementAtOrDefault(0) ?? "";
        var param1 = partedText.ElementAtOrDefault(1) ?? "";

        if (!_telegramService.IsFromSudo)
        {
            await _telegramService.SendTextMessageAsync("Harap melakukan Registrasi sebelum GBan");
            return;
        }

        if (param1 == "sync")
        {
            await _telegramService.SendTextMessageAsync("Memperbarui cache..");
            await _globalBanService.UpdateCache();

            await _telegramService.EditMessageTextAsync("Selesai memperbarui..");

            return;
        }

        if (_telegramService.ReplyToMessage != null)
        {
            var replyToMessage = _telegramService.ReplyToMessage;
            userId = replyToMessage.From.Id;
            reason = msg.Text;

            if (reason.IsNotNullOrEmpty())
                reason = reason
                    .Replace(param0, "", StringComparison.CurrentCulture)
                    .Trim();
        }
        else
        {
            if (param1.IsNullOrEmpty())
            {
                await _telegramService.SendTextMessageAsync("Balas seseorang yang mau di ban");

                return;
            }

            userId = param1.ToInt64();
            reason = msg.Text;

            if (reason.IsNotNullOrEmpty())
                reason = reason
                    .Replace(param0, "", StringComparison.CurrentCulture)
                    .Replace(param1, "", StringComparison.CurrentCulture)
                    .Trim();
        }

        Log.Information("Execute Global Ban");
        await _telegramService.SendTextMessageAsync($"Memeriksa pemblokiran UserId: {userId}");

        var banData = new GlobalBanItem()
        {
            UserId = userId,
            BannedBy = fromId,
            BannedFrom = chatId,
            ReasonBan = reason.IsNullOrEmpty() ? "General SpamBot" : reason
        };

        var isBan = await _globalBanService.IsExist(userId);

        if (isBan)
        {
            await _telegramService.EditMessageTextAsync("Pengguna sudah di ban");

            return;
        }

        await _telegramService.EditMessageTextAsync("Menyimpan informasi..");
        var save = await _globalBanService.SaveBanAsync(banData);

        Log.Information("SaveBan: {Save}", save);

        await _telegramService.EditMessageTextAsync("Memperbarui cache.");
        await _globalBanService.UpdateCache(userId);

        await _telegramService.EditMessageTextAsync("Pengguna berhasil di tambahkan.");
    }
}