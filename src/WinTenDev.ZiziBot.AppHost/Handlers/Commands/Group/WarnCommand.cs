using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using SqlKata;
using SqlKata.Execution;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Group;

public class WarnCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public WarnCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        // await WarnMemberAsync();
        await _telegramService.WarnMemberAsync();
    }

    public async Task WarnMemberAsync()
    {
        try
        {
            Log.Information("Prepare Warning Member..");
            var message = _telegramService.Message;
            var repMessage = message.ReplyToMessage;
            var textMsg = message.Text;
            var fromId = message.From.Id;
            var partText = textMsg.Split(" ");
            var reasonWarn = partText.ValueOfIndex(1) ?? "no-reason";
            var user = repMessage.From;
            var userId = user.Id;
            Log.Information("Warning User: {User}", user);

            var warnLimit = 4;
            var warnHistory = await UpdateWarnMemberStat(message);
            var updatedStep = warnHistory.StepCount;
            var lastMessageId = warnHistory.LastWarnMessageId;
            var nameLink = user.GetNameLink();

            var sendText = $"{nameLink} di beri peringatan!." +
                           $"\nPeringatan ke {updatedStep} dari {warnLimit}";

            if (updatedStep == warnLimit) sendText += "\nIni peringatan terakhir!";

            if (!reasonWarn.IsNullOrEmpty())
            {
                sendText += $"\n<b>Reason:</b> {reasonWarn}";
            }

            var muteUntil = DateTime.UtcNow.AddMinutes(3);
            await _telegramService.RestrictMemberAsync(fromId, until: muteUntil);

            if (updatedStep > warnLimit)
            {
                var sendWarn = $"Batas peringatan telah di lampaui." +
                               $"\n{nameLink} di tendang sekarang!";
                await _telegramService.SendTextMessageAsync(sendWarn);

                await _telegramService.KickMemberAsync(userId, true);
                // await _telegramService.UnbanMemberAsync(user);
                await ResetWarnMemberStatAsync(message);

                return;
            }

            var inlineKeyboard = new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Hapus peringatan", $"action remove-warn {user.Id}")
                    }
                }
            );

            await _telegramService.SendTextMessageAsync(sendText, inlineKeyboard);
            await UpdateLastWarnMemberMessageIdAsync(message, _telegramService.SentMessageId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error Warn Member");
        }
    }

    private static async Task<WarnMemberHistory> UpdateWarnMemberStat(Message message)
    {
        var tableName = "warn_member_history";
        var repMessage = message.ReplyToMessage;
        var textMsg = message.Text;
        var partText = textMsg.Split(" ");
        var reasonWarn = partText.ValueOfIndex(1) ?? "no-reason";

        var chatId = repMessage.Chat.Id;
        var fromId = repMessage.From.Id;
        var fromFName = repMessage.From.FirstName;
        var fromLName = repMessage.From.LastName;
        var warnerId = message.From.Id;
        var warnerFName = message.From.FirstName;
        var warnerLName = message.From.LastName;

        var warnHistory = await new Query(tableName)
            .Where("from_id", fromId)
            .Where("chat_id", chatId)
            .ExecForSqLite(true)
            .GetAsync();

        var exist = warnHistory.Any<object>();

        Log.Information("Check Warn Username History: {Exist}", exist);

        if (exist)
        {
            var warnHistories = warnHistory.ToJson().MapObject<List<WarnMemberHistory>>().First();

            Log.Information("Mapped: {V}", warnHistories.ToJson(true));

            var newStep = warnHistories.StepCount + 1;
            Log.Information(
                "New step for {From} is {NewStep}",
                message.From,
                newStep
            );

            var update = new Dictionary<string, object>
            {
                { "first_name", fromFName },
                { "last_name", fromLName },
                { "step_count", newStep },
                { "reason_warn", reasonWarn },
                { "warner_first_name", warnerFName },
                { "warner_last_name", warnerLName },
                { "updated_at", DateTime.UtcNow }
            };

            var insertHit = await new Query(tableName)
                .Where("from_id", fromId)
                .Where("chat_id", chatId)
                .ExecForSqLite(true)
                .UpdateAsync(update);

            Log.Information("Update step: {InsertHit}", insertHit);
        }
        else
        {
            var data = new Dictionary<string, object>
            {
                { "from_id", fromId },
                { "first_name", fromFName },
                { "last_name", fromLName },
                { "step_count", 1 },
                { "reason_warn", reasonWarn },
                { "warner_user_id", warnerId },
                { "warner_first_name", warnerFName },
                { "warner_last_name", warnerLName },
                { "chat_id", message.Chat.Id },
                { "created_at", DateTime.UtcNow }
            };

            var insertHit = await new Query(tableName)
                .ExecForSqLite(true)
                .InsertAsync(data);

            Log.Information("Insert Hit: {InsertHit}", insertHit);
        }

        var updatedHistory = await new Query(tableName)
            .Where("from_id", fromId)
            .Where("chat_id", chatId)
            .ExecForSqLite(true)
            .GetAsync();

        return updatedHistory.ToJson().MapObject<List<WarnMemberHistory>>().First();
    }

    public async Task UpdateLastWarnMemberMessageIdAsync(
        Message message,
        long messageId
    )
    {
        Log.Information("Updating last Warn Member MessageId.");

        var tableName = "warn_member_history";
        var fromId = message.ReplyToMessage.From.Id;
        var chatId = message.Chat.Id;

        var update = new Dictionary<string, object>
        {
            { "last_warn_message_id", messageId },
            { "updated_at", DateTime.UtcNow }
        };

        var insertHit = await new Query(tableName)
            .Where("from_id", fromId)
            .Where("chat_id", chatId)
            .ExecForSqLite(true)
            .UpdateAsync(update);

        Log.Information("Update lastWarn: {InsertHit}", insertHit);
    }

    public static async Task ResetWarnMemberStatAsync(Message message)
    {
        Log.Information("Resetting warn Username step.");

        var tableName = "warn_member_history";
        var fromId = message.ReplyToMessage.From.Id;
        var chatId = message.Chat.Id;

        var update = new Dictionary<string, object>
        {
            { "step_count", 0 },
            { "updated_at", DateTime.UtcNow }
        };

        var insertHit = await new Query(tableName)
            .Where("from_id", fromId)
            .Where("chat_id", chatId)
            .ExecForSqLite(true)
            .UpdateAsync(update);

        Log.Information("Update step: {InsertHit}", insertHit);
    }

    public async Task RemoveWarnMemberStatAsync(int userId)
    {
        Log.Information("Removing warn Member stat.");

        var tableName = "warn_member_history";
        var message = _telegramService.Message;
        var chatId = message.Chat.Id;

        var update = new Dictionary<string, object>
        {
            { "step_count", 0 },
            { "updated_at", DateTime.UtcNow }
        };

        var insertHit = await new Query(tableName)
            .Where("from_id", userId)
            .Where("chat_id", chatId)
            .ExecForSqLite(true)
            .UpdateAsync(update);

        Log.Information("Update step: {InsertHit}", insertHit);
    }
}