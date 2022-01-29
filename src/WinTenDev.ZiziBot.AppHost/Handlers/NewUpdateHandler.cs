using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.Extensions.Logging;
using SerilogTimings;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.Enums;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.ZiziBot.AppHost.Handlers;

public class NewUpdateHandler : IUpdateHandler
{
    private readonly ILogger<NewUpdateHandler> _logger;
    private readonly AfkService _afkService;
    private readonly AntiSpamService _antiSpamService;
    private readonly MataService _mataService;
    private readonly SettingsService _settingsService;
    private readonly TelegramService _telegramService;
    private readonly WordFilterService _wordFilterService;

    private ChatSetting _chatSettings;
    private long _chatId;
    private long _fromId;

    public NewUpdateHandler(
        ILogger<NewUpdateHandler> logger,
        AfkService afkService,
        AntiSpamService antiSpamService,
        MataService mataService,
        SettingsService settingsService,
        TelegramService telegramService,
        WordFilterService wordFilterService
    )
    {
        _logger = logger;
        _afkService = afkService;
        _antiSpamService = antiSpamService;
        _mataService = mataService;
        _telegramService = telegramService;
        _settingsService = settingsService;
        _wordFilterService = wordFilterService;
    }

    public async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        CancellationToken cancellationToken
    )
    {
        if (When.SkipCheck(context))
        {
            _logger.LogDebug("Update handler disabled for some Update");
            return;
        }

        await _telegramService.AddUpdateContext(context);

        _chatId = _telegramService.ChatId;
        _fromId = _telegramService.FromId;

        _telegramService.IsMessageTooOld();

        _chatSettings = await _telegramService.GetChatSetting();

        _logger.LogTrace("NewUpdate: {@V}", _telegramService.Update);

        // Pre-Task is should be awaited.
        var preTaskResult = await RunPreTasks();

        // Last, do additional task which bot may do
        RunPostTasks();

        if (!preTaskResult)
        {
            _logger.LogDebug("Next handler is ignored because pre-task is not success");
            return;
        }

        _logger.LogDebug("Continue to next Handler");

        await next(context, cancellationToken);
    }

    private async Task<bool> RunPreTasks()
    {
        var op = Operation.Begin("Run PreTask for ChatId: {ChatId}", _telegramService.ChatId);

        var hasRestricted = await CheckChatHasRestrictedAsync();
        if (hasRestricted)
        {
            return false;
        }

        var hasSpam = await AntiSpamCheck();
        if (hasSpam.IsAnyBanned)
        {
            return false;
        }

        var hasUsername = await CheckHasUsernameAsync();
        if (!hasUsername)
        {
            return false;
        }

        var hasPhotoProfile = await CheckHasPhotoProfileAsync();
        if (!hasPhotoProfile)
        {
            return false;
        }

        var shouldDelete = await ScanMessageAsync();
        if (shouldDelete)
        {
            return false;
        }

        op.Complete();

        return true;
    }

    private void RunPostTasks()
    {
        var op = Operation.Begin("Run PostTask");

        var nonAwaitTasks = new List<Task>
        {
            EnsureChatHealthAsync(),
            AfkCheck(),
            CheckMataZiziAsync()
        };

        nonAwaitTasks.InBackgroundAll();

        op.Complete();
    }

    #region Pre Task

    private async Task<bool> CheckChatHasRestrictedAsync()
    {
        var op = Operation.Begin("Check Chat Restriction on ChatId:'{ChatId}'", _chatId);
        try
        {
            if (_telegramService.IsPrivateChat)
            {
                op.Complete();
                return false;
            }

            _logger.LogInformation("Starting ensure Chat Restriction");

            var globalRestrict = _telegramService.IsRestricted();
            var isRestricted = _telegramService.IsChatRestricted;

            if (!isRestricted || !globalRestrict) return false;

            _logger.LogWarning("I should leave right now!");
            var msgOut = "Sepertinya saya salah alamat, saya pamit dulu..";

            await _telegramService.SendTextMessageAsync(msgOut);
            await _telegramService.LeaveChat(_chatId);

            op.Complete();
            return true;
        }
        catch
        {
            _logger.LogError("Error when Check Chat Restriction");

            op.Complete();
            return false;
        }
    }

    private async Task<AntiSpamResult> AntiSpamCheck()
    {
        var fromId = _telegramService.FromId;

        if (_telegramService.IsPrivateChat ||
            _telegramService.CheckFromAnonymous() ||
            _telegramService.CheckSenderChannel())
        {
            return new AntiSpamResult
            {
                UserId = fromId,
                MessageResult = string.Empty,
                IsAnyBanned = false,
                IsEs2Banned = false,
                IsCasBanned = false,
                IsSpamWatched = false,
            };
        }

        var antiSpamResult = await _antiSpamService.CheckSpam(_chatId, fromId);

        if (antiSpamResult == null) return null;

        var messageBan = antiSpamResult.MessageResult;

        if (antiSpamResult.IsAnyBanned) await _telegramService.KickMemberAsync(fromId, true);

        await _telegramService.SendTextMessageAsync(messageBan, replyToMsgId: 0);

        return antiSpamResult;
    }

    private async Task<bool> CheckHasUsernameAsync()
    {
        var checkUsername = await _telegramService.RunCheckUserUsername();

        return checkUsername;
    }

    private async Task<bool> CheckHasPhotoProfileAsync()
    {
        if (_telegramService.CallbackQuery != null) return true;

        var checkPhoto = await _telegramService.RunCheckUserProfilePhoto();

        return checkPhoto;
    }

    private async Task<bool> ScanMessageAsync()
    {
        try
        {
            var message = _telegramService.MessageOrEdited;
            if (message == null) return false;

            var messageId = message.MessageId;
            if (!_chatSettings.EnableWordFilterGroupWide)
            {
                _logger.LogDebug("Word Filter on {ChatId} is disabled!", _chatId);
                return false;
            }

            var text = _telegramService.MessageOrEditedText ?? _telegramService.MessageOrEditedCaption;
            if (text.IsNullOrEmpty())
            {
                _logger.LogInformation("No Text at MessageId {MessageId} for scan..", messageId);
                return false;
            }

            if (_telegramService.IsFromSudo && (
                    text.StartsWith("/dkata") ||
                    text.StartsWith("/delkata") ||
                    text.StartsWith("/kata")))
            {
                _logger.LogDebug("Seem User will modify Kata!");
                return false;
            }

            var result = await _wordFilterService.IsMustDelete(text);
            var isMustDelete = result.IsSuccess;

            if (isMustDelete) _logger.LogInformation("Starting scan image if available..");

            _logger.LogInformation("Message {MsgId} IsMustDelete: {IsMustDelete}", messageId, isMustDelete);

            if (isMustDelete)
            {
                _logger.LogDebug("Result: {V}", result.ToJson(true));
                var note = "Pesan di Obrolan di hapus karena terdeteksi filter Kata.\n" + result.Notes;
                await _telegramService.SendEventAsync(note);

                await _telegramService.DeleteAsync(messageId);
            }

            return isMustDelete;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occured when run {V}", nameof(ScanMessageAsync).Humanize());
            return false;
        }
    }

    #endregion Pre Task

    #region Post Task

    private async Task AfkCheck()
    {
        var sw = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting check AFK");

            var message = _telegramService.MessageOrEdited;

            if (!_chatSettings.EnableAfkStat)
            {
                _logger.LogInformation("Afk Stat is disabled in this Group!");
                return;
            }

            if (_telegramService.MessageOrEdited == null) return;

            if (message.Text != null && message.Text.StartsWith("/afk")) return;

            if (message.ReplyToMessage != null)
            {
                var repMsg = message.ReplyToMessage;
                var repFromId = repMsg.From.Id;

                var isAfkReply = await _afkService.GetAfkById(repFromId);
                if (isAfkReply == null)
                {
                    _logger.LogDebug("No AFK data for '{FromId}' because never recorded as AFK", repFromId);
                    return;
                }

                if (isAfkReply.IsAfk)
                {
                    var repNameLink = repMsg.GetFromNameLink();
                    await _telegramService.SendTextMessageAsync($"{repNameLink} sedang afk");
                }
            }

            var fromAfk = await _afkService.GetAfkById(_fromId);
            if (fromAfk == null)
            {
                _logger.LogDebug("No AFK data for '{FromId}' because never recorded as AFK", _fromId);
                return;
            }

            if (fromAfk.IsAfk)
            {
                var nameLink = message.GetFromNameLink();
                // var currentAfk = await _afkService.GetAfkById(fromId);

                if (fromAfk.IsAfk) await _telegramService.SendTextMessageAsync($"{nameLink} sudah tidak afk");

                var data = new Dictionary<string, object>
                {
                    { "chat_id", _chatId },
                    { "user_id", _fromId },
                    { "is_afk", 0 },
                    { "afk_reason", "" },
                    { "afk_end", DateTime.Now }
                };

                await _afkService.SaveAsync(data);
                await _afkService.UpdateAfkByIdCacheAsync(_fromId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AFK Check - Error occured on {ChatId}", _chatId);
        }

        _logger.LogDebug("AFK check completed. In {Elapsed}", sw.Elapsed);
        sw.Stop();
    }

    private async Task CheckMataZiziAsync()
    {
        var sw = Stopwatch.StartNew();

        try
        {
            if (_telegramService.MessageOrEdited == null) return;

            var message = _telegramService.MessageOrEdited;
            var fromUsername = message.From.Username;
            var fromFName = message.From.FirstName;
            var fromLName = message.From.LastName;

            var chatSettings = await _telegramService.GetChatSetting();
            if (!chatSettings.EnableZiziMata)
            {
                _logger.LogInformation("MataZizi is disabled in this Group!. Completed in {Elapsed}", sw.Elapsed);
                sw.Stop();
                return;
            }

            var botUser = await _telegramService.GetMeAsync();

            _logger.LogInformation("Starting SangMata check..");

            var hitActivityCache = await _mataService.GetMataCore(_fromId);
            if (hitActivityCache.IsNull)
            {
                _logger.LogInformation("This may first Hit from User {V}. In {V1}", _fromId, sw.Elapsed);

                await _mataService.SaveMataAsync(_fromId, new HitActivity
                {
                    ViaBot = botUser.Username,
                    UpdateType = _telegramService.Update.Type,
                    FromId = message.From.Id,
                    FromFirstName = message.From.FirstName,
                    FromLastName = message.From.LastName,
                    FromUsername = message.From.Username,
                    FromLangCode = message.From.LanguageCode,
                    ChatId = message.Chat.Id,
                    ChatUsername = message.Chat.Username,
                    ChatType = message.Chat.Type,
                    ChatTitle = message.Chat.Title,
                    Timestamp = DateTime.Now
                });

                return;
            }

            var changesCount = 0;
            var msgBuild = new StringBuilder();

            msgBuild.AppendLine("😽 <b>MataZizi</b>");
            msgBuild.AppendLine($"<b>UserID:</b> {_fromId}");

            var hitActivity = hitActivityCache.Value;

            if (fromUsername != hitActivity.FromUsername)
            {
                _logger.LogDebug("Username changed detected!");
                msgBuild.AppendLine($"Mengubah Username menjadi @{fromUsername}");
                changesCount++;
            }

            if (fromFName != hitActivity.FromFirstName)
            {
                _logger.LogDebug("First Name changed detected!");
                msgBuild.AppendLine($"Mengubah nama depan menjadi {fromFName}");
                changesCount++;
            }

            if (fromLName != hitActivity.FromLastName)
            {
                _logger.LogDebug("Last Name changed detected!");
                msgBuild.AppendLine($"Mengubah nama belakang menjadi {fromLName}");
                changesCount++;
            }

            if (changesCount > 0)
            {
                await _telegramService.SendTextMessageAsync(msgBuild.ToString().Trim());

                await _mataService.SaveMataAsync(_fromId, new HitActivity
                {
                    ViaBot = botUser.Username,
                    UpdateType = _telegramService.Update.Type,
                    FromId = message.From.Id,
                    FromFirstName = message.From.FirstName,
                    FromLastName = message.From.LastName,
                    FromUsername = message.From.Username,
                    FromLangCode = message.From.LanguageCode,
                    ChatId = message.Chat.Id,
                    ChatUsername = message.Chat.Username,
                    ChatType = _telegramService.Chat.Type,
                    ChatTitle = message.Chat.Title,
                    Timestamp = DateTime.Now
                });

                _logger.LogDebug("Complete update Cache");
            }

            _logger.LogInformation("MataZizi completed in {Elapsed}. Changes: {ChangesCount}", sw.Elapsed, changesCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error SangMata");
        }

        sw.Stop();
    }

    private async Task EnsureChatHealthAsync()
    {
        var chat = _telegramService.Chat;
        var chatType = chat.Type.Humanize();
        var fromFullName = _telegramService.From.GetFullName();
        var isBotAdmin = await _telegramService.CheckBotAdmin();

        var op = Operation.Begin("Ensure Chat Settings for ChatId: '{ChatId}'", _chatId);

        var data = new Dictionary<string, object>
        {
            { "chat_id", _chatId },
            { "chat_title", chat.Title ?? fromFullName },
            { "chat_type", chatType },
            { "is_admin", isBotAdmin }
        };

        var saveSettings = await _settingsService.SaveSettingsAsync(data);
        op.Complete("SaveSettings", saveSettings);

        _logger.LogDebug("Ensure Settings for ChatID: '{ChatId}' result {SaveSettings}", _chatId, saveSettings);
    }

    #endregion Post Task
}