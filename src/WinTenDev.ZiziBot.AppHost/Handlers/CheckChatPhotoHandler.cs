using System.Threading;
using System.Threading.Tasks;
using SerilogTimings;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers;

public class CheckChatPhotoHandler : IUpdateHandler
{
    private readonly TelegramService _telegramService;
    private readonly ChatPhotoCheckService _chatPhotoCheckService;

    public CheckChatPhotoHandler(
        TelegramService telegramService,
        ChatPhotoCheckService chatPhotoCheckService
    )
    {
        _telegramService = telegramService;
        _chatPhotoCheckService = chatPhotoCheckService;
    }

    public async Task HandleAsync(IUpdateContext context, UpdateDelegate next, CancellationToken cancellationToken)
    {
        if (When.SkipCheck(context))
        {
            await next(context, cancellationToken);

            return;
        }

        await _telegramService.AddUpdateContext(context);
        var userId = _telegramService.FromId;
        var chatId = _telegramService.ChatId;

        var op = Operation.Begin("Check Chat Photo Handler for UserId: {UserId}", userId);

        var checkPhoto = await _chatPhotoCheckService.CheckChatPhoto(
        chatId: chatId,
        userId: userId,
        funcCallbackAnswer: answer => _telegramService.AnswerCallbackAsync(answer));

        op.Complete();

        if (checkPhoto)
        {
            await next(context, cancellationToken);
        }
    }
}