using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers;

class StickerHandler : IUpdateHandler
{
    private readonly TelegramService _telegramService;

    public StickerHandler(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public async Task HandleAsync(IUpdateContext context, UpdateDelegate next, CancellationToken cancellationToken)
    {
        await _telegramService.AddUpdateContext(context);

        var msg = context.Update.Message;
        var incomingSticker = msg.Sticker;

        var chat = await _telegramService.GetChat();
        var stickerSetName = chat.StickerSetName ?? "EvilMinds";
        var stickerSet = await _telegramService.Client.GetStickerSetAsync(stickerSetName, cancellationToken: cancellationToken);

        var similarSticker = stickerSet.Stickers.FirstOrDefault(
        sticker => incomingSticker.Emoji.Contains(sticker.Emoji)
        );

        var replySticker = similarSticker ?? stickerSet.Stickers.First();

        var stickerFileId = replySticker.FileId;

        await _telegramService.Client.SendStickerAsync(chatId: chat.Id, stickerFileId);
    }
}