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
    private readonly ChatService _chatService;
    private readonly ChatPhotoCheckService _chatPhotoCheckService;
    private readonly MataService _mataService;
    private readonly PrivilegeService _privilegeService;
    private readonly SettingsService _settingsService;
    private readonly TelegramService _telegramService;
    private readonly CheckUsernameService _checkUsernameService;
    private readonly WordFilterService _wordFilterService;

    private ChatSetting _chatSettings;
    private long _chatId;
    private long _fromId;

    public NewUpdateHandler(
        ILogger<NewUpdateHandler> logger,
        AfkService afkService,
        AntiSpamService antiSpamService,
        ChatService chatService,
        ChatPhotoCheckService chatPhotoCheckService,
        MataService mataService,
        PrivilegeService privilegeService,
        SettingsService settingsService,
        TelegramService telegramService,
        CheckUsernameService checkUsernameService,
        WordFilterService wordFilterService
    )
    {
        _logger = logger;
        _afkService = afkService;
        _antiSpamService = antiSpamService;
        _chatService = chatService;
        _chatPhotoCheckService = chatPhotoCheckService;
        _mataService = mataService;
        _privilegeService = privilegeService;
        _telegramService = telegramService;
        _settingsService = settingsService;
        _checkUsernameService = checkUsernameService;
        _wordFilterService = wordFilterService;
    }

    public async Task HandleAsync(IUpdateContext context, UpdateDelegate next, CancellationToken cancellationToken)
    {
        if (When.SkipCheck(context))
        {
            _logger.LogWarning("Update handler disabled for Channel");
            return;
        }

        await _telegramService.AddUpdateContext(context);

        _chatId = _telegramService.ChatId;
        _fromId = _telegramService.FromId;

        _telegramService.IsMessageTooOld();

        _chatSettings = await _telegramService.GetChatSetting();

        _logger.LogDebug("NewUpdate: {@V}", _telegramService.Context.Update);

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

        var checkUsernameResult = await CheckHasUsernameAsync();
        if (!checkUsernameResult)
        {
            return false;
        }

        var hasAntiSpamCheck = await AntiSpamCheck();
        if (hasAntiSpamCheck.IsAnyBanned)
        {
            return false;
        }

        var isDelete = await ScanMessageAsync();
        if (isDelete)
        {
            return false;
        }

        var hasPhotoProfile = await CheckHasPhotoProfileAsync();
        if (!hasPhotoProfile)
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
        var antiSpamResult = await _antiSpamService.CheckSpam(fromId);

        if (antiSpamResult == null) return null;

        var messageBan = antiSpamResult.MessageResult;

        if (antiSpamResult.IsAnyBanned) await _telegramService.KickMemberAsync(fromId, true);

        await _telegramService.SendTextMessageAsync(messageBan, replyToMsgId: 0);

        return antiSpamResult;
    }

    private async Task<bool> CheckHasUsernameAsync()
    {
        if (_telegramService.IsPrivateChat)
        {
            _logger.LogWarning("Check Username is disabled for Private chat!");
            return true;
        }

        if (!_chatSettings.EnableWarnUsername)
        {
            _logger.LogWarning("Check Username on ChatId: '{ChatId}' is disabled by settings", _chatId);
            return true;
        }

        if (_telegramService.HasUsername)
        {
            _logger.LogWarning("Check Username on ChatId: '{ChatId}' should stop because UserId '{UserId}' has Username",
            _chatId, _fromId);

            return true;
        }

        await _telegramService.RunCheckUsername();

        return false;
    }

    private async Task<bool> CheckHasPhotoProfileAsync()
    {
        var checkPhoto = await _chatPhotoCheckService.CheckChatPhoto(_chatId, _fromId,
        answer => _telegramService.AnswerCallbackAsync(answer));

        return checkPhoto;
    }

    private async Task<bool> ScanMessageAsync()
    {
        try
        {
            var callbackQuery = _telegramService.CallbackQuery;

            if (callbackQuery != null)
            {
                _logger.LogWarning("Look this message is callbackQuery!");
                return false;
            }

            var message = _telegramService.MessageOrEdited;

            if (message == null)
            {
                _logger.LogInformation("This Message don't contain any Message");
                return false;
            }

            var msgId = message.MessageId;

            if (!_chatSettings.EnableWordFilterGroupWide)
            {
                _logger.LogDebug("Word Filter on {ChatId} is disabled!", _chatId);
                return false;
            }

            var text = message.Text ?? message.Caption;
            if (text.IsNullOrEmpty())
            {
                _logger.LogInformation("No message Text for scan..");
                return false;
            }

            var result = await _wordFilterService.IsMustDelete(text);
            var isMustDelete = result.IsSuccess;

            if (isMustDelete) _logger.LogInformation("Starting scan image if available..");

            _logger.LogInformation("Message {MsgId} IsMustDelete: {IsMustDelete}", msgId, isMustDelete);

            if (isMustDelete)
            {
                _logger.LogDebug("Result: {V}", result.ToJson(true));
                var note = "Pesan di Obrolan di hapus karena terdeteksi filter Kata.\n" + result.Notes;
                await _telegramService.SendEventAsync(note);

                await _telegramService.DeleteAsync(message.MessageId);
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
            _logger.LogError(ex, "Error occured when run {V}", nameof(AfkCheck).Humanize());
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
                    MessageType = message.Type.ToString(),
                    FromId = message.From.Id,
                    FromFirstName = message.From.FirstName,
                    FromLastName = message.From.LastName,
                    FromUsername = message.From.Username,
                    FromLangCode = message.From.LanguageCode,
                    ChatId = message.Chat.Id,
                    ChatUsername = message.Chat.Username,
                    ChatType = message.Chat.Type.ToString(),
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
                    // _telegramService.SetChatCache(fromId.ToString(), new HitActivity
                    {
                        ViaBot = botUser.Username,
                        MessageType = message.Type.ToString(),
                        FromId = message.From.Id,
                        FromFirstName = message.From.FirstName,
                        FromLastName = message.From.LastName,
                        FromUsername = message.From.Username,
                        FromLangCode = message.From.LanguageCode,
                        ChatId = message.Chat.Id,
                        ChatUsername = message.Chat.Username,
                        ChatType = message.Chat.Type.ToString(),
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
        var message = _telegramService.AnyMessage;
        var chatType = message.Chat.Type.Humanize();
        var fromFullName = _telegramService.From.GetFullName();
        var isBotAdmin = await _telegramService.CheckBotAdmin();

        var op = Operation.Begin("Ensure Chat Settings for ChatId: '{ChatId}'", _chatId);

        var data = new Dictionary<string, object>
        {
            { "chat_id", _chatId },
            { "chat_title", message.Chat.Title ?? fromFullName },
            { "chat_type", chatType },
            { "is_admin", isBotAdmin }
        };

        var saveSettings = await _settingsService.SaveSettingsAsync(data);
        op.Complete("SaveSettings", saveSettings);

        _logger.LogDebug("Ensure Settings for ChatID: '{ChatId}' result {SaveSettings}", _chatId, saveSettings);
    }

    #endregion Post Task
}