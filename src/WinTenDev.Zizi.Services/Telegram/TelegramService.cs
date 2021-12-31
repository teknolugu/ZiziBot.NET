using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using Serilog;
using SerilogTimings;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.IO;
using WinTenDev.Zizi.Utils.Telegram;
using File=System.IO.File;

namespace WinTenDev.Zizi.Services.Telegram;

public class TelegramService
{
    private readonly ChatService _chatService;
    private readonly CommonConfig _commonConfig;
    private readonly BotService _botService;
    private readonly SettingsService _settingsService;
    private readonly PrivilegeService _privilegeService;
    private readonly CheckUsernameService _checkUsernameService;

    public bool IsNoUsername { get; private set; }
    public bool HasUsername { get; private set; }
    public bool IsFromSudo { get; private set; }
    public bool IsPrivateChat { get; set; }
    public bool IsChatRestricted { get; set; }

    public int SentMessageId { get; set; }
    public int EditedMessageId { get; private set; }
    public int CallBackMessageId { get; set; }

    public long FromId { get; set; }
    public long ReplyFromId { get; set; }
    public long ChatId { get; set; }
    public long ReducedChatId { get; set; }

    private string ChatTitle { get; set; }
    private string AppendText { get; set; }
    private string TimeInit { get; set; }
    private string TimeProc { get; set; }

    public string AnyMessageText { get; set; }
    public string MessageOrEditedText { get; set; }
    public string[] MessageTextParts { get; set; }

    public User From { get; set; }
    public Chat Chat { get; set; }

    public Message AnyMessage { get; set; }
    public Message Message { get; set; }
    public Message EditedMessage { get; set; }
    public Message MessageOrEdited { get; set; }
    public Message ReplyToMessage { get; set; }
    public Message SentMessage { get; set; }
    public Message CallbackMessage { get; set; }
    public CallbackQuery CallbackQuery { get; set; }
    public IUpdateContext Context { get; private set; }
    public ITelegramBotClient Client { get; private set; }

    public TelegramService(
        ChatService chatService,
        CommonConfig commonConfig,
        BotService botService,
        SettingsService settingsService,
        PrivilegeService privilegeService,
        CheckUsernameService checkUsernameService
    )
    {
        _chatService = chatService;
        _commonConfig = commonConfig;
        _botService = botService;
        _settingsService = settingsService;
        _privilegeService = privilegeService;
        _checkUsernameService = checkUsernameService;
    }

    public Task AddUpdateContext(IUpdateContext updateContext)
    {
        var op = Operation.Begin("Add context for '{ChatId}'", ChatId);

        Context = updateContext;
        Client = updateContext.Bot.Client;

        CallbackQuery = updateContext.Update.CallbackQuery;

        Message = updateContext.Update.Message;
        EditedMessage = updateContext.Update.EditedMessage;
        CallbackMessage = CallbackQuery?.Message;
        MessageOrEdited = Message ?? EditedMessage;

        ReplyToMessage = MessageOrEdited?.ReplyToMessage;
        AnyMessage = CallbackMessage ?? Message ?? EditedMessage;

        TimeInit = Message?.Date.GetDelay();

        ReplyFromId = ReplyToMessage?.From?.Id ?? 0;

        From = CallbackQuery?.From ?? MessageOrEdited?.From;
        Chat = CallbackQuery?.Message?.Chat ?? MessageOrEdited?.Chat;

        FromId = From?.Id ?? 0;
        ChatId = Chat?.Id ?? 0;
        ReducedChatId = ChatId.ReduceChatId();
        ChatTitle = Chat?.Title ?? From?.FirstName;

        IsNoUsername = CheckUsername();
        HasUsername = !CheckUsername();
        IsFromSudo = CheckFromSudoer();
        IsChatRestricted = CheckRestriction();
        IsPrivateChat = CheckIsChatPrivate();

        AnyMessageText = AnyMessage.Text;
        MessageOrEditedText = MessageOrEdited?.Text;
        MessageTextParts = MessageOrEditedText?.SplitText(" ")
            .Where(s => s.IsNotNullOrEmpty()).ToArray();

        op.Complete();

        return Task.CompletedTask;
    }

    #region Chat

    public bool IsRestricted()
    {
        var isRestricted = _commonConfig.IsRestricted;
        Log.Debug("Global Restriction: {IsRestricted}", isRestricted);

        return isRestricted;
    }

    public bool CheckRestriction()
    {
        return _chatService.CheckChatRestriction(ChatId);
    }

    public async Task<string> GetMentionAdminsStr()
    {
        var admins = await GetChatAdmin();

        var strBuild = new StringBuilder();
        foreach (var admin in admins)
        {
            var user = admin.User;
            var nameLink = user.Id.GetMention();

            strBuild.Append(nameLink);
        }

        return strBuild.ToString();
    }

    public async Task LeaveChat(long chatId = 0)
    {
        try
        {
            var chatTarget = chatId;
            if (chatId == 0) chatTarget = Message.Chat.Id;

            Log.Information("Leaving from {ChatTarget}", chatTarget);
            await Client.LeaveChatAsync(chatTarget);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error LeaveChat");
        }
    }

    public async Task<long> GetMemberCount()
    {
        return await _chatService.GetMemberCountAsync(ChatId);
    }

    public async Task<Chat> GetChat()
    {
        return await _chatService.GetChatAsync(ChatId);
    }

    public async Task<ChatSetting> GetChatSetting()
    {
        var chatSetting = await _settingsService.GetSettingsByGroup(ChatId);

        return chatSetting;
    }

    public async Task<ChatMember[]> GetChatAdmin()
    {
        var chatAdmin = await _privilegeService.GetChatAdministratorsAsync(ChatId);

        return chatAdmin;
    }

    public async Task<bool> IsAdminOrPrivateChat()
    {
        var adminOrPrivateChat = await _privilegeService.IsAdminOrPrivateChat(ChatId, FromId);

        return adminOrPrivateChat;
    }

    public bool IsGroupChat()
    {
        var chat = AnyMessage.Chat;
        var isGroupChat = chat.Type == ChatType.Group || chat.Type == ChatType.Supergroup;

        Log.Debug("Chat with ID {ChatId} IsGroupChat? {IsGroupChat}", ChatId, isGroupChat);
        return isGroupChat;
    }

    public string GetCommand()
    {
        var cmd = string.Empty;

        if (MessageOrEditedText.StartsWith("/"))
        {
            cmd = MessageTextParts.ValueOfIndex(0);
        }

        return cmd;
    }

    #endregion Chat

    #region Privilege

    public bool CheckFromSudoer()
    {
        return _privilegeService.IsFromSudo(FromId);
    }

    public async Task<bool> CheckBotAdmin()
    {
        Log.Debug("Starting check is Bot Admin");

        if (IsPrivateChat) return false;

        var isBotAdmin = await _privilegeService.IsBotAdminAsync(ChatId);

        return isBotAdmin;
    }

    public async Task<bool> CheckFromAdmin()
    {
        Log.Debug("Starting check is From Admin");

        if (IsPrivateChat) return false;

        var isFromAdmin = await _privilegeService.IsAdminAsync(ChatId, FromId);

        return isFromAdmin;
    }

    private bool CheckIsChatPrivate()
    {
        var isPrivate = Chat.Type == ChatType.Private;

        Log.Debug("Chat {ChatId} IsPrivateChat => {IsPrivate}", ChatId, isPrivate);
        return isPrivate;
    }

    #endregion Privilege

    #region Bot

    public async Task<bool> IsAnyMe(IEnumerable<User> users)
    {
        Log.Information("Checking is added me?");

        var me = await GetMeAsync();

        var isMe = (from user in users where user.Id == me.Id select user.Id == me.Id).FirstOrDefault();
        Log.Information("Is added me? {IsMe}", isMe);

        return isMe;
    }

    public async Task<User> GetMeAsync()
    {
        var getMe = await _botService.GetMeAsync();
        return getMe;
    }

    public async Task<bool> IsBeta()
    {
        return await _botService.IsBeta();
    }

    public async Task<string> GetUrlStart(string param)
    {
        return await _botService.GetUrlStart(param);
    }

    public async Task NotifyPendingCount(int pendingLimit = 100)
    {
        var webhookInfo = await Client.GetWebhookInfoAsync();

        var pendingCount = webhookInfo.PendingUpdateCount;
        if (pendingCount != pendingLimit)
        {
            await SendEventCoreAsync($"Pending Count larger than {pendingLimit}", repToMsgId: 0);
        }
        else
        {
            Log.Information("Pending count is under {PendingLimit}", pendingLimit);
        }
    }

    public async Task<CallbackResult> CallbackAnswerAsync(CallbackAnswer callbackAnswer)
    {
        CallbackResult callbackResult = new();

        var answerModes = callbackAnswer.CallbackAnswerModes;
        var answerCallback = callbackAnswer.CallbackAnswerText;
        var answerReplyMarkup = callbackAnswer.CallbackAnswerInlineMarkup;
        var muteTimeSpan = callbackAnswer.MuteMemberTimeSpan;

        await Parallel.ForEachAsync(answerModes, async (answerMode, cancel) =>
            // foreach (var answerMode in answerModes)
        {
            switch (answerMode)
            {
                case CallbackAnswerMode.AnswerCallback:
                    await AnswerCallbackQueryAsync(answerCallback, showAlert: true);
                    break;

                case CallbackAnswerMode.SendMessage:
                    callbackResult.UpdatedMessage = await SendTextMessageAsync(answerCallback, answerReplyMarkup);
                    break;

                case CallbackAnswerMode.EditMessage:
                    var messageMarkup = callbackAnswer.CallbackAnswerInlineMarkup;
                    callbackResult.UpdatedMessage = await EditMessageTextAsync(answerCallback, messageMarkup);
                    break;

                case CallbackAnswerMode.BanMember:
                    break;

                case CallbackAnswerMode.MuteMember:
                    await RestrictMemberAsync(FromId, until: muteTimeSpan.ToDateTime());
                    break;

                case CallbackAnswerMode.DeleteMessage:
                    var messageId = callbackAnswer.CallbackDeleteMessageId;
                    await DeleteAsync(messageId);
                    break;

                default:
                    Log.Debug("No Callback Answer mode for this section. {@V}", answerMode);
                    break;
            }
        });

        return callbackResult;
    }

    #endregion Bot

    #region EventLog

    public async Task SendEventAsync(string text = "N/A", int repToMsgId = -1)
    {
        Log.Information("Sending Event to Global and Local..");
        var globalLogTarget = BotSettings.BotChannelLogs;
        var currentSetting = await GetChatSetting();
        var chatLogTarget = currentSetting.EventLogChatId;

        var listLogTarget = new List<long>();

        if (globalLogTarget != -1) listLogTarget.Add(globalLogTarget);
        if (chatLogTarget != 0) listLogTarget.Add(chatLogTarget);
        Log.Debug("Channel Targets: {ListLogTarget}", listLogTarget);

        foreach (var chatId in listLogTarget)
        {
            await SendEventCoreAsync(text, chatId, true, repToMsgId: repToMsgId);
        }
    }

    public async Task SendEventCoreAsync(string additionalText = "N/A",
        long customChatId = 0, bool disableWebPreview = false, int repToMsgId = -1)
    {
        var message = MessageOrEdited;
        var chatTitle = Chat.Title ?? From.FirstName;
        var fromNameLink = message.From.GetNameLink();
        var msgLink = message.GetMessageLink();

        var sendLog = "🐾 <b>EventLog Preview</b>" +
                      $"\nGroup: <code>{ChatId}</code> - {chatTitle}" +
                      $"\nFrom: <code>{FromId}</code> - {fromNameLink}" +
                      $"\n<a href='{msgLink}'>Go to Message</a>" +
                      $"\nNote: {additionalText}" +
                      $"\n\n#{message.Type} => #ID{ReducedChatId}";

        await SendTextMessageAsync(
        sendLog, customChatId: customChatId, disableWebPreview: disableWebPreview, replyToMsgId: repToMsgId);
    }

    #endregion

    #region Message

    public bool IsMessageTooOld(int offset = 5)
    {
        var isOld = false;

        if (Message != null)
        {
            var date = Message.Date;

            // Skip older than offset minutes
            var prev10M = DateTime.UtcNow.AddMinutes(-offset);
            Log.Debug("Msg Date: {V}", date.ToString("yyyy-MM-dd hh:mm:ss tt zz"));
            Log.Debug("Prev 10m: {V}", prev10M.ToString("yyyy-MM-dd hh:mm:ss tt zz"));

            isOld = prev10M > date;
        }

        Log.Debug("Is MessageId {MessageId} too old? => {IsOld}", Message?.MessageId, isOld);

        return isOld;
    }

    public async Task<string> DownloadFileAsync(string prefixName)
    {
        var fileId = Message.GetFileId();
        if (fileId.IsNullOrEmpty()) fileId = Message.ReplyToMessage.GetFileId();

        Log.Information("Getting file by FileId {FileId}", fileId);
        var file = await Client.GetFileAsync(fileId);

        var filePath = file.FilePath;
        Log.Debug("DownloadFile: {@File}", file);
        var fileName = $"{prefixName}_{filePath}";

        fileName = $"Storage/Caches/{fileName}".EnsureDirectory();

        await using var fileStream = File.OpenWrite(fileName);
        await Client.DownloadFileAsync(file.FilePath, fileStream);
        Log.Information("File saved to {FileName}", fileName);

        return fileName;
    }

    public async Task<Message> SendTextMessageAsync(
        string sendText,
        IReplyMarkup replyMarkup = null,
        int replyToMsgId = -1,
        long customChatId = -1,
        bool disableWebPreview = false
    )
    {
        TimeProc = AnyMessage.Date.GetDelay();

        if (sendText.IsNotNullOrEmpty()) sendText += $"\n\n⏱ <code>{TimeInit} s</code> | ⌛️ <code>{TimeProc} s</code>";

        var chatTarget = AnyMessage.Chat.Id;
        if (customChatId < -1) chatTarget = customChatId;

        if (replyToMsgId == -1) replyToMsgId = MessageOrEdited.MessageId;

        if (sendText.IsNullOrEmpty())
        {
            Log.Warning("Message can't be send because null");
            return null;
        }

        try
        {
            Log.Information("Sending message to {ChatTarget}", chatTarget);
            SentMessage = await Client.SendTextMessageAsync(
            chatId: chatTarget,
            text: sendText,
            parseMode: ParseMode.Html,
            replyMarkup: replyMarkup,
            replyToMessageId: replyToMsgId,
            disableWebPagePreview: disableWebPreview
            );

            return SentMessage;
        }
        catch (Exception exception1)
        {
            if (!exception1.IsErrorAsWarning())
            {
                Log.Error(exception1, "Send Message to {ChatTarget} Exception_1", chatTarget);

                return SentMessage;
            }

            Log.Warning("Failed when trying send Message to {ChatTarget}. {Message}",
            chatTarget, exception1.Message);

            try
            {
                Log.Information("Try Sending message to {ChatTarget} without reply to Msg Id", chatTarget);
                SentMessage = await Client.SendTextMessageAsync(
                chatId: chatTarget,
                text: sendText,
                parseMode: ParseMode.Html,
                replyMarkup: replyMarkup
                );

                return SentMessage;
            }
            catch (Exception exception2)
            {
                Log.Error(exception2, "Send Message to {ChatTarget} Exception_2", chatTarget);
            }
        }

        Log.Information("Sent Message Text: {SentMessageId}", SentMessageId);

        return SentMessage;
    }

    public async Task<Message> SendMediaAsync(
        string fileId,
        MediaType mediaType,
        string caption = "",
        IReplyMarkup replyMarkup = null,
        int replyToMsgId = -1
    )
    {
        Log.Information("Sending media: {MediaType}, fileId: {FileId} to {Id}", mediaType, fileId, Message.Chat.Id);

        TimeProc = Message.Date.GetDelay();
        if (caption.IsNotNullOrEmpty()) caption += $"\n\n⏱ <code>{TimeInit} s</code> | ⌛️ <code>{TimeProc} s</code>";

        switch (mediaType)
        {
            case MediaType.Document:
                SentMessage = await Client.SendDocumentAsync(chatId: ChatId, document: fileId, caption: caption,
                parseMode: ParseMode.Html, replyMarkup: replyMarkup, replyToMessageId: replyToMsgId);
                break;

            case MediaType.LocalDocument:
                var fileName = Path.GetFileName(fileId);
                await using (var fs = File.OpenRead(fileId))
                {
                    var inputOnlineFile = new InputOnlineFile(fs, fileName);
                    SentMessage = await Client.SendDocumentAsync(chatId: ChatId, document: inputOnlineFile, caption: caption,
                    parseMode: ParseMode.Html, replyMarkup: replyMarkup, replyToMessageId: replyToMsgId);
                }

                break;

            case MediaType.Photo:
                SentMessage = await Client.SendPhotoAsync(ChatId, fileId, caption: caption, ParseMode.Html,
                replyMarkup: replyMarkup, replyToMessageId: replyToMsgId);
                break;

            case MediaType.Video:
                SentMessage = await Client.SendVideoAsync(ChatId, video: fileId, caption: caption,
                parseMode: ParseMode.Html, replyMarkup: replyMarkup, replyToMessageId: replyToMsgId);
                break;

            default:
                Log.Information("Media unknown: {MediaType}", mediaType);
                return null;
        }

        Log.Information("SendMedia: {MessageId}", SentMessage.MessageId);

        return SentMessage;
    }

    public async Task SendMediaGroupAsync(List<IAlbumInputMedia> listAlbum)
    {
        var itemCount = "item".ToQuantity(listAlbum.Count);
        Log.Information("Sending Media Group to {ChatId} with {ItemCount}", ChatId, itemCount);
        var message = await Client.SendMediaGroupAsync(ChatId, listAlbum);
        Log.Debug("Send Media Group Result on '{ChatId}' => {Message}", ChatId, message.Length > 0);
    }

    public async Task<Message> EditMessageTextAsync(string sendText, InlineKeyboardMarkup replyMarkup = null,
        bool disableWebPreview = true)
    {
        TimeProc = Message.Date.GetDelay();

        if (sendText.IsNotNullOrEmpty()) sendText += $"\n\n⏱ <code>{TimeInit} s</code> | ⌛️ <code>{TimeProc} s</code>";

        var targetMessageId = SentMessage.MessageId;

        Log.Information("Updating message {SentMessageId} on {ChatId}", targetMessageId, ChatId);
        try
        {
            SentMessage = await Client.EditMessageTextAsync(
            ChatId,
            targetMessageId,
            sendText,
            ParseMode.Html,
            replyMarkup: replyMarkup,
            disableWebPagePreview: disableWebPreview
            );

            return SentMessage;
        }
        catch (Exception ex)
        {
            if (ex.IsErrorAsWarning())
            {
                Log.Warning("Failed when trying edit Message on {ChatTarget}. {Message}",
                ChatId, ex.Message);

                return SentMessage;
            }

            Log.Error(ex, "Error edit message");
        }

        return SentMessage;
    }

    public async Task EditMessageCallback(
        string sendText,
        InlineKeyboardMarkup replyMarkup = null,
        bool disableWebPreview = true
    )
    {
        try
        {
            Log.Information("Editing {CallBackMessageId}", CallBackMessageId);
            await Client.EditMessageTextAsync(
            ChatId,
            CallBackMessageId,
            sendText,
            ParseMode.Html,
            replyMarkup: replyMarkup,
            disableWebPagePreview: disableWebPreview
            );
        }
        catch (Exception e)
        {
            Log.Error(e, "Error EditMessage");
        }
    }

    public async Task AppendTextAsync(string sendText, InlineKeyboardMarkup replyMarkup = null)
    {
        if (string.IsNullOrEmpty(AppendText))
        {
            Log.Information("Sending new message");
            AppendText = sendText;
            await SendTextMessageAsync(AppendText, replyMarkup);
        }
        else
        {
            Log.Information("Next, edit existing message");
            AppendText += $"\n{sendText}";
            await EditMessageTextAsync(AppendText, replyMarkup);
        }
    }

    public async Task DeleteAsync(int messageId = -1, int delay = 0)
    {
        Thread.Sleep(delay);

        var msgId = messageId != -1 ? messageId : SentMessageId;

        try
        {
            Log.Information("Delete MsgId: {MsgId} on ChatId: {ChatId}", msgId, ChatId);
            await Client.DeleteMessageAsync(ChatId, msgId);
        }
        catch (Exception ex)
        {
            if (ex.IsErrorAsWarning())
            {
                Log.Warning(ex, "Error Delete MessageId {MessageId} On ChatId {ChatId}", msgId, ChatId);
            }
            else
            {
                Log.Error(ex, "Error Delete MessageId {MessageId} On ChatId {ChatId}", msgId, ChatId);
            }
        }
    }

    public async Task ForwardMessageAsync(int messageId = -1)
    {
        var fromChatId = Message.Chat.Id;
        var msgId = Message.MessageId;
        if (messageId != -1) msgId = messageId;
        var chatId = _commonConfig.ChannelLogs;
        await Client.ForwardMessageAsync(chatId, fromChatId, msgId);
    }

    public async Task AnswerCallbackQueryAsync(string text, bool showAlert = false)
    {
        try
        {
            var callbackQueryId = Context.Update.CallbackQuery.Id;
            await Client.AnswerCallbackQueryAsync(callbackQueryId, text, showAlert);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error Answer Callback");
        }
    }

    public void ResetTime()
    {
        Log.Information("Resetting time..");

        var msgDate = Message.Date;
        var currentDate = DateTime.UtcNow;
        msgDate = msgDate.AddSeconds(-currentDate.Second);
        msgDate = msgDate.AddMilliseconds(-currentDate.Millisecond);
        TimeInit = msgDate.GetDelay();
    }

    #endregion Message

    #region Member

    public async Task<bool> KickMemberAsync(long userId, bool unban = false)
    {
        bool isKicked;

        Log.Information("Kick {UserId} from {ChatId}", userId, ChatId);
        try
        {
            await Client.BanChatMemberAsync(ChatId, userId, DateTime.Now);

            if (unban) await UnBanMemberAsync(userId);
            isKicked = true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error Kick Member");
            isKicked = false;
        }

        return isKicked;
    }

    public async Task UnbanMemberAsync(User user = null)
    {
        var idTarget = user.Id;
        Log.Information("Unban {IdTarget} from {ChatId}", idTarget, ChatId);
        try
        {
            await Client.UnbanChatMemberAsync(ChatId, idTarget);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "UnBan Member");
            await SendTextMessageAsync(ex.Message);
        }
    }

    public async Task UnBanMemberAsync(long userId = -1)
    {
        Log.Information("Unban {UserId} from {ChatId}", userId, ChatId);
        try
        {
            await Client.UnbanChatMemberAsync(ChatId, userId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "UnBan Member");
            await SendTextMessageAsync(ex.Message);
        }
    }

    public async Task<RequestResult> PromoteChatMemberAsync(long userId)
    {
        var requestResult = new RequestResult();
        try
        {
            await Client.PromoteChatMemberAsync(
            Message.Chat.Id,
            userId,
            false,
            false,
            false,
            true,
            true,
            true,
            true);

            requestResult.IsSuccess = true;
        }
        catch (ApiRequestException apiRequestException)
        {
            Log.Error(apiRequestException, "Error Promote Member");
            requestResult.IsSuccess = false;
            requestResult.ErrorCode = apiRequestException.ErrorCode;
            requestResult.ErrorMessage = apiRequestException.Message;
        }

        return requestResult;
    }

    public async Task<RequestResult> DemoteChatMemberAsync(long userId)
    {
        var requestResult = new RequestResult();
        try
        {
            await Client.PromoteChatMemberAsync(
            Message.Chat.Id,
            userId,
            false,
            false,
            false,
            false,
            false,
            false,
            false);

            requestResult.IsSuccess = true;
        }
        catch (ApiRequestException apiRequestException)
        {
            requestResult.IsSuccess = false;
            requestResult.ErrorCode = apiRequestException.ErrorCode;
            requestResult.ErrorMessage = apiRequestException.Message;

            Log.Error(apiRequestException, "Error Demote Member");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Demote Member Ex");
        }

        return requestResult;
    }

    public async Task<TelegramResult> RestrictMemberAsync(long userId, bool unMute = false, DateTime until = default)
    {
        var tgResult = new TelegramResult();

        try
        {
            var untilDate = until;
            if (until == default) untilDate = DateTime.UtcNow.AddDays(366);

            Log.Information("Restricting UserId: '{UserId}'@'{ChatId}', UnMute: '{UnMute}' until {UntilDate}",
            userId, ChatId, unMute, untilDate);

            var permission = new ChatPermissions
            {
                CanSendMessages = unMute,
                CanSendMediaMessages = unMute,
                CanSendOtherMessages = unMute,
                CanAddWebPagePreviews = unMute,
                CanChangeInfo = unMute,
                CanInviteUsers = unMute,
                CanPinMessages = unMute,
                CanSendPolls = unMute
            };

            Log.Debug("ChatPermissions: {@V}", permission);

            if (unMute) untilDate = DateTime.UtcNow;

            await Client.RestrictChatMemberAsync(ChatId, userId, permission, untilDate);

            tgResult.IsSuccess = true;

            Log.Debug("MemberID {UserId} muted until {UntilDate}", userId, untilDate);
        }
        catch (Exception ex)
        {
            Log.Error(ex.Demystify(), "Error restrict userId: {UserId} on {ChatId}", userId, ChatId);
            var exceptionMsg = ex.Message;
            if (exceptionMsg.Contains("CHAT_ADMIN_REQUIRED")) Log.Debug("I'm must Admin on this Group!");

            tgResult.IsSuccess = false;
            tgResult.Exception = ex;
        }

        return tgResult;
    }

    #endregion Member

    #region Username

    public bool CheckUsername()
    {
        var userId = From.Id;
        var ignored = new[]
        {
            "777000"
        };

        var match = ignored.FirstOrDefault(id => id == userId.ToString());
        if (!match.IsNotNullOrEmpty()) return From.Username == null;

        Log.Information("This user true Ignored!");
        return false;
    }

    public async Task RunCheckUsername()
    {
        var warnLimit = 30;
        var lastMessageId = 0;
        var sw = Stopwatch.StartNew();

        var chatSettings = await GetChatSetting();
        if (!chatSettings.EnableWarnUsername)
        {
            Log.Information("Warn Username is disabled on ChatID '{ChatId}'", ChatId);
            return;
        }

        if (HasUsername)
        {
            Log.Information("UserID '{FromId}' has set Username", FromId);

            await _checkUsernameService.RemoveAll(ChatId, FromId);

            return;
        }

        var history = await GetUpdateUsername();

        var stepCount = history.StepCount;
        var nameLink = From.GetNameLink();

        if (stepCount > warnLimit)
        {
            await KickMemberAsync(FromId, true);

            var sendWarn = $"Batas peringatan telah di lampaui." +
                           $"\n{nameLink} di tendang sekarang!";

            lastMessageId = (
                await SendTextMessageAsync(sendWarn, disableWebPreview: true, replyToMsgId: 0)
            ).MessageId;

            await ResetWarnUsername();
        }
        else
        {
            var warnFoot = $"Ini peringatan ke {stepCount}";
            if (stepCount == warnLimit) warnFoot = "Ini peringatan terakhir.";

            var sendWarn = $"Hai {nameLink}, kamu belum memasang Username.\n{warnFoot}";

            lastMessageId = (
                await SendTextMessageAsync(sendWarn, disableWebPreview: true, replyToMsgId: 0)
            ).MessageId;
        }

        var addMinutes = TimeUtil.GetMuteStep(stepCount);
        var muteTime = DateTime.Now.AddMinutes(addMinutes);
        await RestrictMemberAsync(FromId, until: muteTime);

        await _checkUsernameService.UpdateLastMessageId(ChatId, FromId, lastMessageId);

        Log.Information("Username Verify completed in {Elapsed}", sw.Elapsed);
        sw.Stop();
    }

    private async Task<WarnUsernameHistory> GetUpdateUsername()
    {
        var stepCount = 1;
        var lastMessageId = 0;
        var history = await _checkUsernameService.GetHistory(ChatId, FromId);

        if (history != null)
        {
            stepCount = history.StepCount++;
            lastMessageId = history.LastWarnMessageId;

            if (history.LastWarnMessageId > 0)
            {
                await DeleteAsync(history.LastWarnMessageId);
            }
        }

        await _checkUsernameService.SaveUsername(new WarnUsernameHistory()
        {
            FromId = FromId,
            FirstName = From.FirstName,
            LastName = From.LastName ?? "",
            ChatId = ChatId,
            LastWarnMessageId = lastMessageId,
            CreatedAt = DateTime.Now,
            StepCount = stepCount
        });

        var updatedHistory = await _checkUsernameService.GetHistory(ChatId, FromId);

        return updatedHistory;
    }

    protected virtual void OnCompleteCheckUsername(EventArgs e)
    {
    }

    public async Task ResetWarnUsername()
    {
        await _checkUsernameService.ResetWarnUsername(FromId);
    }

    #endregion Username
}