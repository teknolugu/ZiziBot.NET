using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using SerilogTimings;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Tags;

public class TagsCommand : CommandBase
{
    private readonly TagsService _tagsService;
    private readonly SettingsService _settingsService;
    private readonly TelegramService _telegramService;

    public TagsCommand(
        TelegramService telegramService,
        TagsService tagsService,
        SettingsService settingsService
    )
    {
        _telegramService = telegramService;
        _tagsService = tagsService;
        _settingsService = settingsService;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);
        var chatId = _telegramService.ChatId;
        var op = Operation.Begin("/tags command on ChatId {ChatId}", chatId);
        var msg = _telegramService.Message;

        await _telegramService.DeleteAsync(msg.MessageId);
        var sentMessage = await _telegramService.SendTextMessageAsync("🔄 Loading tags..", replyToMsgId: 0);
        var tagsData = await _tagsService.GetTagsByGroupAsync(chatId);

        if (!tagsData.Any())
        {
            await _telegramService.EditMessageTextAsync("Sepertinya belum ada Tags di obrolan ini.");
            await DeletePrevTagsList(sentMessage.MessageId);
            op.Complete();

            return;
        }

        Log.Debug("Building Tags message for ChatId: '{ChatId}'", chatId);
        var tagsStr = new StringBuilder();
        foreach (var tag in tagsData)
        {
            tagsStr.Append('#').Append(tag.Tag).Append(' ');
        }

        var sendText = $"#️⃣<b> {tagsData.Count()} Tags</b>\n" +
                       $"\n{tagsStr.ToTrimmedString()}";

        await _telegramService.EditMessageTextAsync(sendText);
        await DeletePrevTagsList(sentMessage.MessageId);
        op.Complete();
    }

    private async Task DeletePrevTagsList(long messageId)
    {
        var chatId = _telegramService.ChatId;
        var currentSetting = await _settingsService.GetSettingsByGroup(chatId);
        var lastTagsMsgId = currentSetting.LastTagsMessageId;

        if (lastTagsMsgId.ToInt() > 0)
            await _telegramService.DeleteAsync(lastTagsMsgId.ToInt());

        Log.Information("LastTagsMsgId: {LastTagsMsgId}", lastTagsMsgId);

        await _settingsService.UpdateCell(chatId, "last_tags_message_id", messageId);
    }
}