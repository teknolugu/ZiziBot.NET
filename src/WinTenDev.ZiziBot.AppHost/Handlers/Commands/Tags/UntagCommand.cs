using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Tags;

public class UntagCommand : CommandBase
{
    private readonly TagsService _tagsService;
    private readonly TelegramService _telegramService;

    public UntagCommand(
        TagsService tagsService,
        TelegramService telegramService
    )
    {
        _tagsService = tagsService;
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var tagVal = _telegramService.MessageTextParts.ValueOfIndex(1);
        var chatId = _telegramService.ChatId;
        var fromId = _telegramService.FromId;
        var sendText = "Hanya Admin Grup yang dapat menghapus Tag.";

        if (
            !_telegramService.IsFromSudo &&
            !await _telegramService.CheckUserPermission()
        )
        {
            await _telegramService.SendTextMessageAsync(sendText);

            Log.Debug
            (
                "UserId {UserId} don't have privilege for remove tag on ChatId {ChatId}!",
                fromId, chatId
            );
            return;
        }

        if (tagVal.IsNullOrEmpty())
        {
            await _telegramService.SendTextMessageAsync("Tag apa yg mau di hapus?");
            return;
        }

        await _telegramService.SendTextMessageAsync("Memeriksa..");
        var isExist = await _tagsService.IsExist(chatId, tagVal);

        if (isExist)
        {
            Log.Information("Sedang menghapus tag {TagVal}", tagVal);
            var unTag = await _tagsService.DeleteTag(chatId, tagVal);

            if (unTag)
            {
                sendText = $"Hapus tag {tagVal} berhasil";
            }

            await _telegramService.EditMessageTextAsync(sendText);
            await _tagsService.UpdateCacheAsync(chatId);
        }
        else
        {
            await _telegramService.EditMessageTextAsync($"Tag {tagVal} tidak di temukan");
        }
    }
}
