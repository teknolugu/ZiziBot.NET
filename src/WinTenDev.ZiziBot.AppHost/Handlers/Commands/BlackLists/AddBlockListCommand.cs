using System;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.BlackLists;

public class AddBlockListCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly BlockListService _blockListService;

    public AddBlockListCommand(
        TelegramService telegramService,
        BlockListService blockListService
    )
    {
        _telegramService = telegramService;
        _blockListService = blockListService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var url = _telegramService.MessageTextParts.ValueOfIndex(1);

        if (url.IsNullOrEmpty())
        {
            await _telegramService.SendTextMessageAsync($"Apa url yang mau di tambahkan?");
            return;
        }

        await _telegramService.SendTextMessageAsync($"Sedang membaca URL: {url}");
        var listUrl = await _blockListService.ParseList(url);

        var result = $"⛓ Source: {url}" +
                     $"\n📜 {listUrl.Name}" +
                     $"\n📦 {listUrl.Source}" +
                     $"\n📅 {listUrl.LastUpdate}" +
                     $"\n🔍 {listUrl.DomainCount}";

        await _telegramService.EditMessageTextAsync(result);

        await _blockListService.SaveBlockList
        (
            new BlockList()
            {
                ChatId = _telegramService.ChatId,
                FromId = _telegramService.FromId,
                UrlSource = url,
                CreatedAt = DateTime.Now
            }
        );
    }
}
