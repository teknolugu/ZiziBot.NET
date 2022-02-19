using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Words;

public class AddKataCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly WordFilterService _wordFilterService;

    public AddKataCommand(
        TelegramService telegramService,
        WordFilterService wordFilterService
    )
    {
        _telegramService = telegramService;
        _wordFilterService = wordFilterService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var chatId = _telegramService.ChatId;
        var fromId = _telegramService.FromId;

        var messageTextParts = _telegramService.MessageTextParts;
        var word = messageTextParts.ElementAtOrDefault(1) ?? string.Empty;
        var paramOption = messageTextParts.ElementAtOrDefault(2) ?? string.Empty;

        var isGlobalBlock = false;

        var isSudoer = _telegramService.IsFromSudo;

        if (!isSudoer)
        {
            await _telegramService.DeleteSenderMessageAsync();
            Log.Information("Currently add Kata is limited only Sudo");
            return;
        }

        if (word.IsValidUrl())
        {
            word = word.ParseUrl().Path;
        }

        var where = new Dictionary<string, object>() { { "word", word } };

        if (paramOption.IsContains("-"))
        {
            if (paramOption.IsContains("g"))// Global
            {
                isGlobalBlock = true;
                await _telegramService.AppendTextAsync("Kata ini akan di blokir Global!");
            }
        }

        if (!paramOption.IsContains("g"))
        {
            @where.Add("chat_id", chatId);
        }

        if (!word.IsNotNullOrEmpty())
        {
            await _telegramService.SendTextMessageAsync("Apa kata yg mau di blok?");
        }
        else
        {

            var isExist = await _wordFilterService.IsExistAsync(@where);
            if (isExist)
            {
                await _telegramService.AppendTextAsync("Kata sudah di tambahkan");
            }
            else
            {
                await _telegramService.AppendTextAsync("Sedang menambahkan kata");
                await _wordFilterService.SaveWordAsync(new WordFilter()
                {
                    Word = word,
                    ChatId = chatId,
                    IsGlobal = isGlobalBlock,
                    FromId = fromId,
                    CreatedAt = DateTime.Now
                });

                await _telegramService.AppendTextAsync("Kata berhasil di tambahkan");
                await _wordFilterService.UpdateWordListsCache();
            }
        }

        await _telegramService.DeleteAsync(delay: 3000);
    }
}