using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SerilogTimings;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Tags;

public class FindTagCommand : IUpdateHandler
{
    private readonly TelegramService _telegramService;
    private readonly ILogger<FindTagCommand> _logger;
    private readonly TagsService _tagsService;

    public FindTagCommand(
        ILogger<FindTagCommand> logger,
        TelegramService telegramService,
        TagsService tagsService
    )
    {
        _logger = logger;
        _tagsService = tagsService;
        _telegramService = telegramService;
    }

    public async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        CancellationToken cancellationToken
    )
    {
        const int limitedCount = 5;

        await _telegramService.AddUpdateContext(context);
        var chatId = _telegramService.ChatId;
        var op = Operation.Begin("Find Tag for ChatId: {0}", chatId);

        var chatSettings = await _telegramService.GetChatSetting();

        if (!chatSettings.EnableFindTags)
        {
            _logger.LogInformation("Find Tags is disabled on ChatId {ChatId}", chatId);
            return;
        }

        var parts = _telegramService.MessageTextParts
            .Where(s => s.Contains('#'))
            .Distinct()
            .ToList();

        _logger.LogDebug("AllTags: {0}", parts);
        var tags = await _tagsService.GetTagsByGroupAsync(chatId);

        var step = 1;

        foreach (var part in parts)
        {
            _logger.LogInformation("Processing : {0} => ", part);
            var trimTag = part.TrimStart('#');

            var tagData = tags.FirstOrDefault(x => x.Tag == trimTag);

            if (tagData == null)
            {
                _logger.LogDebug("Tag {0} is not found.", trimTag);
                continue;
            }

            _logger.LogTrace("Data of tag: {0} => {1}", trimTag, tagData);

            ProcessingTag(tagData).InBackground();

            step++;
            if (step >= limitedCount) break;
        }

        if (step >= limitedCount)
        {
            const string sendText = "Karena alasan kinerja, Zizi membatasi pemanggilan 5 Tag bersamaan";
            await _telegramService.SendTextMessageAsync(sendText);
        }

        op.Complete();
    }

    private async Task ProcessingTag(CloudTag tagData)
    {
        var content = tagData.Content;
        var buttonStr = tagData.BtnData;
        var typeData = tagData.TypeData;
        var idData = tagData.FileId;

        var buttonMarkup = InlineKeyboardMarkup.Empty();

        if (!buttonStr.IsNullOrEmpty())
        {
            buttonMarkup = buttonStr.ToReplyMarkup(2);
        }

        if (typeData != MediaType.Unknown)
        {
            await _telegramService.SendMediaAsync
            (
                fileId: idData,
                mediaType: typeData,
                caption: content,
                replyMarkup: buttonMarkup
            );
        }
        else
        {
            await _telegramService.SendTextMessageAsync
            (
                sendText: content,
                replyMarkup: buttonMarkup
            );
        }
    }
}