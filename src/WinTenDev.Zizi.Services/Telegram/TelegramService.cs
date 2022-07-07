using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Hangfire;
using Humanizer;
using Microsoft.Extensions.Options;
using Serilog;
using SerilogTimings;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using TgBotFramework;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Externals;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.IO;
using WinTenDev.Zizi.Utils.Telegram;
using File=System.IO.File;

namespace WinTenDev.Zizi.Services.Telegram;

public class TelegramService
{
    private readonly IBackgroundJobClient _backgroundJob;
    private readonly TgBotConfig _tgBotConfig;
    private readonly EventLogConfig _eventLogConfig;
    private readonly FeatureService _featureService;
    private readonly PrivilegeService _privilegeService;
    private readonly UserProfilePhotoService _userProfilePhotoService;
    private readonly StepHistoriesService _stepHistoriesService;

    internal CommonConfig CommonConfig { get; }
    internal EnginesConfig EnginesConfig { get; }
    internal IServiceProvider ServiceProvider { get; }
    internal AfkService AfkService { get; }
    internal AnimalsService AnimalsService { get; }
    internal AntiSpamService AntiSpamService { get; }
    internal BotService BotService { get; }
    internal DeepAiService DeepAiService { get; }
    internal EventLogService EventLogService { get; }
    internal FloodCheckService FloodCheckService { get; }
    internal GlobalBanService GlobalBanService { get; }
    internal LocalizationService LocalizationService { get; }
    internal MataService MataService { get; }
    internal MessageHistoryService MessageHistoryService { get; }
    internal NewChatMembersService NewChatMembersService { get; set; }
    internal NotesService NotesService { get; }
    internal OptiicDevService OptiicDevService { get; set; }
    internal SettingsService SettingsService { get; }
    internal RssService RssService { get; }
    internal WordFilterService WordFilterService { get; }

    public ChatService ChatService { get; }

    public bool IsNoUsername { get; private set; }
    public bool HasUsername { get; private set; }
    public bool IsFromSudo { get; private set; }
    public bool IsPrivateChat { get; set; }
    public bool IsGroupChat { get; set; }
    public bool IsPublicGroup { get; set; }
    public bool IsPrivateGroup { get; set; }
    public bool IsChannel { get; set; }
    public bool IsChatRestricted { get; set; }

    [Obsolete("Please read value from SentMessage")]
    public int SentMessageId { get; set; }

    public int EditedMessageId { get; set; }
    public int CallBackMessageId { get; set; }

    public long FromId { get; set; }
    public long ReplyFromId { get; set; }
    public long ChatId { get; set; }
    public long ReducedChatId { get; set; }

    public string BotUsername { get; set; }
    public string ChatTitle { get; set; }
    public string FromNameLink { get; set; }
    private string AppendText { get; set; }
    internal string TimeInit { get; set; }
    internal string TimeProc { get; set; }

    public string AnyMessageText { get; set; }
    public string MessageOrEditedText { get; set; }
    public string MessageOrEditedCaption { get; set; }
    public string CallbackQueryData { get; set; }
    public string[] MessageTextParts { get; set; }
    public string[] CallbackQueryDatas { get; set; }

    internal string InlineQueryCmd { get; set; }
    internal string InlineQueryValue { get; set; }
    internal List<string> InlineQueryValues { get; set; }

    public User From { get; set; }
    public Chat Chat { get; set; }
    public Chat SenderChat { get; set; }

    public DateTime MessageDate { get; set; }
    public DateTime? MessageEditDate { get; set; }
    public DateTime MessageDateOrEditDate { get; set; }
    private TimeSpan TimeInitSpan { get; set; }
    private TimeSpan TimeProcSpan { get; set; }
    public TimeSpan KickTimeOffset { get; set; }

    public Message AnyMessage { get; set; }
    public Message Message { get; set; }
    public Message EditedMessage { get; set; }
    public Message MessageOrEdited { get; set; }
    public Message ReplyToMessage { get; set; }
    public Message SentMessage { get; set; }
    public Message CallbackMessage { get; set; }
    public Message ChannelPost { get; set; }
    public Message EditedChannelPost { get; set; }
    public Message ChannelOrEditedPost { get; set; }

    public ChosenInlineResult ChosenInlineResult { get; set; }
    public InlineQuery InlineQuery { get; set; }
    public CallbackQuery CallbackQuery { get; set; }
    public IUpdateContext Context { get; private set; }
    public Update Update { get; private set; }
    public ITelegramBotClient Client { get; private set; }
    public ChatMemberUpdated MyChatMember { get; set; }

    internal bool HasChatJoinRequest => ChatJoinRequest != null;
    internal ChatJoinRequest ChatJoinRequest { get; set; }

    public TelegramService(
        IBackgroundJobClient backgroundJob,
        IOptionsSnapshot<EnginesConfig> engOptions,
        IOptionsSnapshot<EventLogConfig> eventLogConfig,
        IOptionsSnapshot<CommonConfig> commonConfig,
        IOptionsSnapshot<TgBotConfig> tgBotConfig,
        IServiceProvider serviceProvider,
        AfkService afkService,
        AnimalsService animalsService,
        AntiSpamService antiSpamService,
        ChatService chatService,
        BotService botService,
        DeepAiService deepAiService,
        EventLogService eventLogService,
        FeatureService featureService,
        FloodCheckService floodCheckServiceService,
        GlobalBanService globalBanService,
        LocalizationService localizationService,
        MataService mataService,
        MessageHistoryService messageHistoryService,
        NewChatMembersService newChatMembersService,
        NotesService notesService,
        OptiicDevService optiicDevService,
        SettingsService settingsService,
        PrivilegeService privilegeService,
        RssService rssService,
        UserProfilePhotoService userProfilePhotoService,
        StepHistoriesService stepHistoriesService,
        WordFilterService wordFilterService
    )
    {
        _backgroundJob = backgroundJob;
        _eventLogConfig = eventLogConfig.Value;
        _featureService = featureService;
        _privilegeService = privilegeService;
        _tgBotConfig = tgBotConfig.Value;
        _userProfilePhotoService = userProfilePhotoService;
        _stepHistoriesService = stepHistoriesService;

        CommonConfig = commonConfig.Value;
        EnginesConfig = engOptions.Value;
        ServiceProvider = serviceProvider;
        AfkService = afkService;
        AnimalsService = animalsService;
        AntiSpamService = antiSpamService;
        BotService = botService;
        DeepAiService = deepAiService;
        EventLogService = eventLogService;
        ChatService = chatService;
        FloodCheckService = floodCheckServiceService;
        GlobalBanService = globalBanService;
        LocalizationService = localizationService;
        MataService = mataService;
        MessageHistoryService = messageHistoryService;
        NewChatMembersService = newChatMembersService;
        NotesService = notesService;
        OptiicDevService = optiicDevService;
        RssService = rssService;
        SettingsService = settingsService;
        WordFilterService = wordFilterService;
    }

    public Task AddUpdateContext(UpdateContext context)
    {
        ChatId = context.ChatId.Identifier ?? 0;

        Client = context.Client;
        Update = context.Update;

        BotUsername = _tgBotConfig.Username;

        AddUpdate(context.Update);

        return Task.CompletedTask;
    }

    public Task AddUpdateContext(IUpdateContext updateContext)
    {
        var op = Operation.Begin("Add context for '{ChatId}'", ChatId);

        Context = updateContext;
        Update = updateContext.Update;
        Client = updateContext.Bot.Client;

        BotUsername = Context.Bot.Username;

        AddUpdate(Update);

        KickTimeOffset = TimeSpan.FromMinutes(1);

        var fromLanguageCode = From?.LanguageCode ?? "en";

        op.Complete();

        return Task.CompletedTask;
    }

    public Task AddUpdate(Update update)
    {
        var op = Operation.Begin("Adding Update: '{UpdateId}'", update.Id);

        CallbackQuery = Update.CallbackQuery;
        InlineQuery = Update.InlineQuery;
        ChosenInlineResult = update.ChosenInlineResult;

        ChatJoinRequest = Update.ChatJoinRequest;

        MyChatMember = Update.MyChatMember;
        Message = Update.Message;
        EditedMessage = Update.EditedMessage;
        ChannelPost = Update.ChannelPost;
        EditedChannelPost = Update.EditedChannelPost;

        MessageOrEdited = Message ?? EditedMessage;
        ChannelOrEditedPost = ChannelPost ?? EditedChannelPost;
        CallbackMessage = CallbackQuery?.Message;

        ReplyToMessage = MessageOrEdited?.ReplyToMessage;
        AnyMessage = CallbackMessage ?? Message ?? EditedMessage;

        ReplyFromId = ReplyToMessage?.From?.Id ?? 0;

        From = ChannelOrEditedPost?.From ??
               MyChatMember?.From ?? ChosenInlineResult?.From ?? InlineQuery?.From ?? CallbackQuery?.From ?? ChatJoinRequest?.From ?? MessageOrEdited?.From;
        Chat = ChannelOrEditedPost?.Chat ?? MyChatMember?.Chat ?? CallbackQuery?.Message?.Chat ?? ChatJoinRequest?.Chat ?? MessageOrEdited?.Chat;
        SenderChat = MessageOrEdited?.SenderChat;
        MessageDate = MyChatMember?.Date ?? CallbackQuery?.Message?.Date ?? MessageOrEdited?.Date ?? DateTime.Now;
        MessageEditDate = MessageOrEdited?.EditDate;
        MessageDateOrEditDate = MessageEditDate ?? MessageDate;

        TimeInit = MessageDate.GetDelay();

        FromId = From?.Id ?? 0;
        ChatId = Chat?.Id ?? 0;
        ReducedChatId = ChatId.ReduceChatId();
        ChatTitle = Chat?.Title ?? From?.GetFullName();
        FromNameLink = From?.GetNameLink();

        IsNoUsername = CheckUsername();
        HasUsername = !CheckUsername();
        IsFromSudo = CheckFromSudoer();
        IsPrivateChat = CheckIsPrivateChat();
        IsGroupChat = CheckIsGroupChat();

        IsPublicGroup = Chat is { Username: {}, Type: ChatType.Group or ChatType.Supergroup };
        IsPrivateGroup = !IsPublicGroup;
        IsChannel = Chat?.Type is ChatType.Channel;

        AnyMessageText = AnyMessage?.Text;
        MessageOrEditedText = MessageOrEdited?.Text;
        MessageOrEditedCaption = MessageOrEdited?.Caption;

        CallbackQueryData = CallbackQuery?.Data;
        CallbackQueryDatas = CallbackQueryData?.Split(' ');

        InlineQueryValues = InlineQuery?.Query.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        InlineQueryCmd = InlineQueryValues?.FirstOrDefault();
        InlineQueryValue = InlineQueryValues?.Skip(1).JoinStr(" ");

        MessageTextParts = MessageOrEditedText?.SplitText(" ")
            .Where(s => s.IsNotNullOrEmpty())
            .ToArray();

        KickTimeOffset = TimeSpan.FromMinutes(1);

        var fromLanguageCode = From?.LanguageCode ?? "en";

        op.Complete();

        return Task.CompletedTask;
    }

    #region Chat

    public async Task<bool> CheckChatRestriction()
    {
        if (IsPrivateChat ||
            CallbackQuery != null ||
            InlineQuery != null ||
            ChosenInlineResult != null) return false;

        var isShouldLeave = ChatService.CheckChatRestriction(ChatId);

        if (!isShouldLeave) return false;

        Log.Warning("I should leave right now!");
        var me = await BotService.GetMeAsync();

        var sendText = "Untuk mendapatkan pengalaman lingkungan yang lebih stabil, " +
                       "silakan gunakan @MissZiziBot untuk Grup Anda." +
                       $"\n<b>{me.GetFullName()}</b> masih tahap pengembangan jadi akses nya dibatasi." +
                       "\n\nTerima kasih sudah menggunakan layanan ZiziBot!";

        await SendTextMessageAsync(sendText, replyToMsgId: 0);
        await LeaveChat(ChatId);

        return true;
    }

    public async Task<string> GetMentionAdminsStr()
    {
        var chatMembers = await GetChatAdmin();
        var adminMentionStr = chatMembers.ToAdminMention();

        return adminMentionStr;
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
        return await ChatService.GetMemberCountAsync(ChatId);
    }

    public async Task<Chat> GetChat()
    {
        return await ChatService.GetChatAsync(ChatId);
    }

    public async Task<ChatMember> GetChatMemberAsync(long userId)
    {
        return await ChatService.GetChatMemberAsync(ChatId, userId);
    }

    public async Task<ChatSetting> GetChatSetting(long chatId = 0)
    {
        var chatIdTarget = ChatId;
        if (chatId != 0) chatIdTarget = chatId;

        var chatSetting = await SettingsService.GetSettingsByGroup(chatIdTarget);

        return chatSetting;
    }

    public async Task<ChatMember[]> GetChatAdmin()
    {
        var chatAdmin = await _privilegeService.GetChatAdministratorsAsync(ChatId);

        return chatAdmin;
    }

    public async Task<string> GetChatAdminList()
    {
        if (Chat.Username != null)
        {
            var channelParticipants = await _privilegeService.GetChatAdministratorsTgApiAsync(ChatId);
            var adminListStr = channelParticipants.ToAdminListStr();

            return adminListStr;
        }
        else
        {
            var adminList = await GetChatAdmin();
            var adminListStr = adminList.ToAdminListStr();
            return adminListStr;
        }
    }

    public async Task SendChatActionAsync(ChatAction action)
    {
        Log.Information(
            "Sending chat Action: '{Action}' to ChatId: '{ChatId}'",
            action,
            ChatId
        );

        await Client.SendChatActionAsync(ChatId, action);
    }

    [Obsolete("Please use separated method IsAdminAsync() and property IsPrivateChat instead of this method")]
    public async Task<bool> IsAdminOrPrivateChat()
    {
        var adminOrPrivateChat = await _privilegeService.IsAdminOrPrivateChat(ChatId, FromId);

        return adminOrPrivateChat;
    }

    public string GetCommand(
        bool withoutSlash = false,
        bool withoutUsername = true
    )
    {
        var cmd = string.Empty;

        if (!MessageOrEditedText?.StartsWith("/") ?? true) return cmd;

        cmd = MessageTextParts.ElementAtOrDefault(0);

        if (withoutSlash) cmd = cmd?.TrimStart('/');
        if (withoutUsername)
            cmd = cmd?.Replace(
                "@" + BotUsername,
                string.Empty,
                StringComparison.CurrentCultureIgnoreCase
            );

        return cmd;
    }

    public T GetCommandParamAt<T>(int index)
    {
        dynamic value = MessageTextParts.Skip(1).ElementAtOrDefault(index);

        return value is null ? default : (T) Convert.ChangeType(value, typeof(T));
    }

    public string GetCommandParam(int index)
    {
        var value = MessageTextParts.Skip(1).ElementAtOrDefault(index);

        return value;
    }

    public bool IsCommand(string command)
    {
        return GetCommand() == command;
    }

    public async Task<string> GetLocalizationString(
        Enum value,
        IEnumerable<(string placeholder, string value)> placeHolders = null
    )
    {
        var settings = await GetChatSetting();

        var langCode = settings.LanguageCode ?? "id";
        var localized = LocalizationService.GetLoc(langCode, value);

        return placeHolders == null ? localized : localized.ResolveVariable(placeHolders);
    }

    #endregion Chat

    #region Privilege

    private bool CheckFromSudoer()
    {
        return _privilegeService.IsFromSudo(FromId);
    }

    public async Task<bool> CheckBotAdmin()
    {
        if (InlineQuery != null)
        {
            Log.Debug("Check Bot Admin disabled because Update is '{UpdateType}'", Update.Type);
            return false;
        }

        Log.Debug("Starting check is Bot Admin");

        if (IsPrivateChat) return false;

        var isBotAdmin = await _privilegeService.IsBotAdminAsync(ChatId);

        return isBotAdmin;
    }

    public async Task<bool> CheckFromAdmin(long userId = -1)
    {
        Log.Debug("Starting check is From Admin");

        if (IsPrivateChat) return false;

        if (userId > 0) FromId = userId;

        var isFromAdmin = await _privilegeService.IsAdminAsync(ChatId, FromId);

        return isFromAdmin;
    }

    public bool CheckFromAnonymous()
    {
        const int anonId = 1087968824;
        var isAnonymous = FromId == anonId;

        Log.Debug(
            "Check is From Anonymous Admin on ChatId: {ChatId}? {IsAnonymous}",
            ChatId,
            isAnonymous
        );

        return isAnonymous;
    }

    public bool CheckSenderChannel()
    {
        var isSenderChannel = SenderChat?.Type == ChatType.Channel;

        Log.Debug(
            "Check is From Sender Channel on ChatId: {ChatId}? {IsAnonymous}",
            ChatId,
            isSenderChannel
        );

        return isSenderChannel;
    }

    private bool CheckIsPrivateChat()
    {
        var isPrivate = Chat?.Type == ChatType.Private;

        Log.Debug(
            "Chat ID '{ChatId}' IsPrivateChat => {IsPrivate}",
            ChatId,
            isPrivate
        );
        return isPrivate;
    }

    public bool CheckIsGroupChat()
    {
        var isGroupChat = Chat?.Type is ChatType.Group or ChatType.Supergroup;

        Log.Debug(
            "Chat ID '{ChatId}' IsGroupChat? {IsGroupChat}",
            ChatId,
            isGroupChat
        );
        return isGroupChat;
    }

    public async Task<bool> CheckFromAdminOrAnonymous()
    {
        if (CheckFromAnonymous()) return true;

        return await CheckFromAdmin();
    }

    public async Task<bool> CheckUserPermission()
    {
        if (IsPrivateChat) return true;
        if (CheckFromAnonymous()) return true;

        return await CheckFromAdmin();
    }

    #endregion Privilege

    #region Bot

    public async Task<ButtonParsed> GetFeatureConfig(string feature = null)
    {
        var featureName = feature ?? GetCommand();
        var featureConfig = await _featureService.GetFeatureConfig(featureName);

        if (featureConfig.IsEnabled)
        {
            var hasAllowed = featureConfig.AllowsAt?.Any(s => s.Contains(ChatId.ToString())) ?? false;

            featureConfig.NextHandler = hasAllowed;

            if (featureConfig.IsApplyRateLimit)
            {
                var nextAvailable = featureConfig.NextAvailable;

                var isNeedCooldown = _featureService.CheckCooldown(
                    new FeatureCooldown()
                    {
                        ChatId = ChatId,
                        UserId = FromId,
                        FeatureName = GetCommand(),
                        LastUsed = DateTime.UtcNow,
                        NextAvailable = nextAvailable
                    }
                );

                if (isNeedCooldown)
                {
                    featureConfig.NextHandler = false;

                    var nextAvailableDate = nextAvailable.ToLocalTime();

                    await SendTextMessageAsync(
                        sendText: $"Fitur '{featureName}' membutuhkan Cooldown sebelum dapat digunakan kembali. Silakan coba lagi setelah {nextAvailableDate}",
                        replyToMsgId: 0,
                        scheduleDeleteAt: DateTime.UtcNow.AddMinutes(10),
                        includeSenderMessage: true,
                        preventDuplicateSend: true
                    );

                    return featureConfig;
                }

                featureConfig.NextHandler = true;
            }

            if (!featureConfig.NextHandler)
            {
                await SendTextMessageAsync(featureConfig.Caption, featureConfig.Markup);
                return featureConfig;
            }
        }

        featureConfig.NextHandler = true;

        return featureConfig;
    }

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
        var getMe = await BotService.GetMeAsync();
        return getMe;
    }

    public async Task<bool> IsBeta()
    {
        return await BotService.IsBeta();
    }

    public async Task<string> GetUrlStart(string param)
    {
        return await BotService.GetUrlStart(param);
    }

    public async Task NotifyPendingCount(int pendingLimit = 100)
    {
        var webhookInfo = await Client.GetWebhookInfoAsync();

        var pendingCount = webhookInfo.PendingUpdateCount;

        if (pendingCount != pendingLimit)
        {
            await SendEventLogCoreAsync($"Pending Count larger than {pendingLimit}", repToMsgId: 0);
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

        await Parallel.ForEachAsync(
            answerModes,
            async (
                answerMode,
                cancel
            ) => {
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

                    case CallbackAnswerMode.KickMember:
                        break;

                    case CallbackAnswerMode.ScheduleKickMember:
                        await ScheduleKickJob(StepHistoryName.HumanVerification);
                        break;

                    default:
                        Log.Debug("No Callback Answer mode for this section. {@V}", answerMode);
                        break;
                }
            }
        );
        return callbackResult;
    }

    #endregion Bot

    #region EventLog

    [Obsolete("Please look EventLogService")]
    private async Task<List<long>> GetEventLogTargets()
    {
        var currentSetting = await GetChatSetting();

        var globalLogTarget = _eventLogConfig.ChannelId;
        var chatLogTarget = currentSetting.EventLogChatId;

        var eventLogTargets = new List<long>()
        {
            globalLogTarget,
            chatLogTarget
        };

        var filteredTargets = eventLogTargets
            .Where(x => x < 0)
            .ToList();

        Log.Debug("List Channel Targets: {ListLogTarget}", filteredTargets);

        return filteredTargets;
    }

    [Obsolete("Please look EventLogService")]
    public async Task SendEventLogAsync(
        string text = "N/A",
        bool withForward = false,
        int forwardMessageId = -1
    )
    {
        Log.Information("Preparing send EventLog");

        var eventLogTargets = await GetEventLogTargets();

        foreach (var chatId in eventLogTargets)
        {
            Message forwardMessage = new();

            if (withForward)
            {
                forwardMessage = await ForwardMessageAsync(messageId: forwardMessageId, toChatId: chatId);
            }

            await SendEventLogCoreAsync(
                additionalText: text,
                customChatId: chatId,
                disableWebPreview: true,
                repToMsgId: forwardMessage.MessageId
            );
        }
    }

    [Obsolete("Please look EventLogService")]
    public async Task SendEventLogCoreAsync(
        string additionalText = "N/A",
        long customChatId = 0,
        bool disableWebPreview = false,
        int repToMsgId = -1
    )
    {
        var message = MessageOrEdited;
        var fromNameLink = From.GetNameLink();
        var chatNameLink = Chat.GetChatNameLink();
        var messageLink = message.GetMessageLink();

        var sendLog = "🐾 <b>EventLog Preview</b>" +
                      $"\n<b>Chat:</b> <code>{ReducedChatId}</code> - {chatNameLink}" +
                      $"\n<b>User:</b> <code>{FromId}</code> - {fromNameLink}" +
                      $"\n<a href='{messageLink}'>Go to Message</a>" +
                      $"\nNote: {additionalText}" +
                      $"\n#{MessageOrEdited.Type} #U{FromId} #C{ReducedChatId}";

        await SendTextMessageAsync(
            sendText: sendLog,
            customChatId: customChatId,
            disableWebPreview: disableWebPreview,
            replyToMsgId: repToMsgId
        );
    }

    [Obsolete("Please look EventLogService")]
    public async Task SendEventLogRawAsync(string sendLog)
    {
        var chatId = _eventLogConfig.ChannelId;

        await SendTextMessageAsync(
            sendText: sendLog,
            customChatId: chatId,
            disableWebPreview: true,
            replyToMsgId: 0
        );
    }

    #endregion EventLog

    #region Message

    public bool IsUpdateTooOld(int offset = 5)
    {
        if (CallbackQuery != null) return false;

        var messageDate = MessageDate;
        var prevDate = DateTime.UtcNow.AddMinutes(-offset);
        var isOld = prevDate > messageDate;

        Log.Debug(
            "UpdateId {UpdateId} with Date: {V}. OffsetDate: {OffsetDate}. Too old? {IsOld}",
            Update.Id,
            messageDate.ToDetailDateTimeString(),
            prevDate.ToDetailDateTimeString(),
            isOld
        );

        return isOld;
    }

    public MessageFlag GetMessageFlag(MessageFlag messageFlag)
    {
        var command = GetCommand(true).Pascalize();
        var fixedFlag = messageFlag == MessageFlag.General ? command.ToEnum(MessageFlag.General) : messageFlag;

        return fixedFlag;
    }

    public async Task<string> DownloadFileAsync(string prefixName)
    {
        var message = Message;
        if (message.ReplyToMessage != null) message = message.ReplyToMessage;

        var fileMetaData = message.GetFileMetadata();
        var fileId = fileMetaData.FileId;

        Log.Information("Getting file by FileId {FileId}", fileId);
        var file = await Client.GetFileAsync(fileId);

        Log.Debug("DownloadFile: {@File}", file);
        var fileType = fileMetaData.Type.Humanize().ToLower();
        var fileUniqueId = file.FileUniqueId;
        var randomStr = StringUtil.GenerateUniqueId(5);
        var fileName = fileMetaData.FileName;

        var fullFilePath = $"{prefixName}_{ReducedChatId}_{fileType}_{fileUniqueId}_{randomStr}#{fileName}";

        fullFilePath = $"Storage/Caches/{fullFilePath}".EnsureDirectory();

        var fileStream = File.OpenWrite(fullFilePath);
        await Client.DownloadFileAsync(file.FilePath!, fileStream);
        Log.Information("File saved to {FileName}", fullFilePath);

        fileStream.Close();

        return fullFilePath;
    }

    public async Task<Message> SendTextMessageAsync(
        string sendText,
        IReplyMarkup replyMarkup = null,
        int replyToMsgId = -1,
        ChatId customChatId = null,
        bool disableWebPreview = false,
        DateTime scheduleDeleteAt = default,
        bool includeSenderMessage = false,
        MessageFlag messageFlag = default,
        bool preventDuplicateSend = false
    )
    {
        TimeProc = MessageDateOrEditDate.GetDelay();
        TimeProcSpan = MessageDateOrEditDate.GetDelaySpan();

        if (sendText.IsNotNullOrEmpty() &&
            CallbackQuery == null)
        {
            Log.Debug("Appending execution time..");
            sendText += $"\n\n⏱ <code>{TimeInit} s</code> | ⌛️ <code>{TimeProc} s</code>";
        }

        var chatTarget = customChatId ?? new ChatId(ChatId);
        // if (customChatId != null) chatTarget = customChatId;

        if (replyToMsgId == -1) replyToMsgId = AnyMessage?.MessageId ?? -1;

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
                allowSendingWithoutReply: true,
                disableWebPagePreview: disableWebPreview
            );

            Log.Information("Sent Message Text: {SentMessageId}", SentMessage?.MessageId);
        }
        catch (Exception exception1)
        {
            if (!exception1.IsErrorAsWarning())
            {
                Log.Error(
                    exception1,
                    "Send Message to {ChatTarget} Exception_1",
                    chatTarget
                );

                return SentMessage;
            }

            Log.Warning(
                "Failed when trying send Message to {ChatTarget}. {Message}",
                chatTarget,
                exception1.Message
            );
        }

        if (scheduleDeleteAt != default)
        {
            SaveToMessageHistory(
                scheduleDeleteAt,
                includeSenderMessage,
                messageFlag
            );
        }

        PreventDuplicateSend(
            preventDuplicateSend,
            messageFlag,
            includeSenderMessage ? 2 : 1
        );

        return SentMessage;
    }

    public async Task<Message> SendMediaAsync(
        string fileId,
        MediaType mediaType,
        string caption = "",
        IReplyMarkup replyMarkup = null,
        int replyToMsgId = -1,
        long customChatId = -1,
        string customFileName = "",
        DateTime scheduleDeleteAt = default,
        bool includeSenderMessage = false,
        MessageFlag messageFlag = default,
        bool preventDuplicateSend = false
    )
    {
        var targetChatId = customChatId == -1 ? ChatId : customChatId;
        Log.Information(
            messageTemplate: "Sending media: {MediaType}, fileId: {FileId} to {ChatId}",
            mediaType,
            fileId,
            targetChatId
        );

        TimeProc = MessageDate.GetDelay();
        TimeProcSpan = MessageDateOrEditDate.GetDelaySpan();

        if (caption.IsNotNullOrEmpty()) caption += $"\n\n⏱ <code>{TimeInit} s</code> | ⌛️ <code>{TimeProc} s</code>";

        switch (mediaType)
        {
            case MediaType.Document:
                var inputFile = new InputOnlineFile(fileId);

                if (fileId.IsValidUrl())
                {
                    Log.Information("Converting URL: '{Url}' to stream", fileId);
                    var stream = await fileId.GetStreamAsync();
                    inputFile = new InputOnlineFile(stream, customFileName);
                }

                SentMessage = await Client.SendDocumentAsync(
                    chatId: targetChatId,
                    document: inputFile,
                    caption: caption,
                    parseMode: ParseMode.Html,
                    replyMarkup: replyMarkup,
                    replyToMessageId: replyToMsgId
                );
                break;

            case MediaType.LocalDocument:
                var fileName = Path.GetFileName(path: fileId);

                await using (var fs = File.OpenRead(path: fileId))
                {
                    var inputOnlineFile = new InputOnlineFile(content: fs, fileName: fileName);

                    SentMessage = await Client.SendDocumentAsync(
                        chatId: targetChatId,
                        document: inputOnlineFile,
                        caption: caption,
                        parseMode: ParseMode.Html,
                        replyMarkup: replyMarkup,
                        replyToMessageId: replyToMsgId
                    );
                }

                break;

            case MediaType.Photo:
                SentMessage = await Client.SendPhotoAsync(
                    chatId: targetChatId,
                    photo: fileId,
                    caption: caption,
                    parseMode: ParseMode.Html,
                    replyMarkup: replyMarkup,
                    replyToMessageId: replyToMsgId
                );
                break;

            case MediaType.Video:
                SentMessage = await Client.SendVideoAsync(
                    chatId: targetChatId,
                    video: fileId,
                    caption: caption,
                    parseMode: ParseMode.Html,
                    replyMarkup: replyMarkup,
                    replyToMessageId: replyToMsgId
                );
                break;

            case MediaType.Sticker:
                SentMessage = await Client.SendStickerAsync(
                    chatId: targetChatId,
                    sticker: fileId,
                    replyMarkup: replyMarkup,
                    replyToMessageId: replyToMsgId
                );

                break;

            default:
                Log.Information(messageTemplate: "Media unknown: {MediaType}", mediaType);
                return null;
        }

        Log.Information(messageTemplate: "SendMedia: {MessageId}", SentMessage?.MessageId);

        if (scheduleDeleteAt != default)
        {
            SaveToMessageHistory(
                deleteAt: scheduleDeleteAt,
                includeSender: includeSenderMessage,
                flag: messageFlag
            );
        }

        PreventDuplicateSend(preventDuplicateSend: preventDuplicateSend, flag: messageFlag);

        return SentMessage;
    }

    public async Task<RequestResult> SendMediaGroupAsync(List<IAlbumInputMedia> listAlbum)
    {
        RequestResult requestResult = new();

        try
        {
            var itemCount = "item".ToQuantity(listAlbum.Count);
            Log.Information(
                "Sending Media Group to {ChatId} with {ItemCount}",
                ChatId,
                itemCount
            );
            var message = await Client.SendMediaGroupAsync(ChatId, listAlbum);
            Log.Debug(
                "Send Media Group Result to '{ChatId}' => {Message}",
                ChatId,
                message.Length > 0
            );

            requestResult.SentMessages = message;
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "Error when Send Media Group to {ChatId}",
                ChatId
            );
            requestResult.ErrorException = exception;
        }

        return requestResult;
    }

    public async Task<Message> EditMessageTextAsync(
        string sendText,
        InlineKeyboardMarkup replyMarkup = null,
        bool disableWebPreview = true,
        DateTime scheduleDeleteAt = default,
        bool includeSenderMessage = false,
        MessageFlag messageFlag = default,
        bool preventDuplicateSend = false
    )
    {
        TimeProc = MessageDateOrEditDate.GetDelay();
        TimeProcSpan = MessageDateOrEditDate.GetDelaySpan();

        if (sendText.IsNotNullOrEmpty()) sendText += $"\n\n⏱ <code>{TimeInit} s</code> | ⌛️ <code>{TimeProc} s</code>";

        var targetMessageId = SentMessage.MessageId;

        Log.Information(
            "Updating message {SentMessageId} on {ChatId}",
            targetMessageId,
            ChatId
        );

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
        }
        catch (Exception exception)
        {
            if (exception.IsErrorAsWarning())
            {
                Log.Warning(
                    "Failed edit MessageId: {MessageId} at ChatId: {ChatId}. {Message}",
                    targetMessageId,
                    ChatId,
                    exception.Message
                );

                return SentMessage;
            }

            Log.Error(
                exception,
                "Error edit messageId: {MessageId} at ChatId: {ChatId}",
                targetMessageId,
                ChatId
            );
        }

        if (scheduleDeleteAt != default)
        {
            SaveToMessageHistory(
                scheduleDeleteAt,
                includeSenderMessage,
                messageFlag
            );
        }

        PreventDuplicateSend(preventDuplicateSend, messageFlag);

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
            Log.Information(
                "Editing Message Callback at ChatId: {ChatId}, MessageId: {CallBackMessageId}",
                ChatId,
                CallBackMessageId
            );

            await Client.EditMessageTextAsync(
                ChatId,
                CallBackMessageId,
                sendText,
                ParseMode.Html,
                replyMarkup: replyMarkup,
                disableWebPagePreview: disableWebPreview
            );
        }
        catch (Exception exception)
        {
            var answerMessage = exception.Message switch
            {
                {} a when a.Contains("too many request") => "Pergunakan tombol Callback secukupnya!",
                {} a when a.Contains("message is not modified") => "Tolong tidak menekan tombol terlalu Ceffat!",
                _ => string.Empty
            };

            if (answerMessage.IsNotNullOrEmpty())
            {
                await DeleteCurrentCallbackMessageAsync();

                await SendTextMessageAsync(
                    answerMessage,
                    scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1)
                );
            }
            else
            {
                Log.Error(
                    exception,
                    "Error Edit Message Callback at ChatId: {ChatId}, MessageId: {CallBackMessageId}",
                    ChatId,
                    CallBackMessageId
                );
            }
        }
    }

    public async Task AppendTextAsync(
        string sendText,
        InlineKeyboardMarkup replyMarkup = null,
        int replyToMsgId = -1,
        DateTime scheduleDeleteAt = default,
        bool includeSenderMessage = false,
        MessageFlag messageFlag = default,
        bool preventDuplicateSend = false,
        bool reappendText = false
    )
    {
        if (reappendText)
            AppendText = AppendText.RemoveLastLines(1);

        if (string.IsNullOrEmpty(AppendText))
        {
            Log.Information("First, Sending new message to ChatId: {ChatId}", ChatId);
            AppendText = sendText;
            await SendTextMessageAsync(
                sendText: AppendText,
                replyMarkup: replyMarkup,
                replyToMsgId: replyToMsgId,
                scheduleDeleteAt: scheduleDeleteAt,
                includeSenderMessage: includeSenderMessage,
                messageFlag: messageFlag,
                preventDuplicateSend: preventDuplicateSend
            );
        }
        else
        {
            Log.Information(
                "Next, edit existing messageId: {MessageId} at ChatId: {ChatId}",
                SentMessage.MessageId,
                ChatId
            );
            AppendText += $"\n{sendText}";
            await EditMessageTextAsync(
                sendText: AppendText,
                replyMarkup: replyMarkup,
                scheduleDeleteAt: scheduleDeleteAt,
                includeSenderMessage: includeSenderMessage,
                messageFlag: messageFlag,
                preventDuplicateSend: preventDuplicateSend
            );
        }
    }

    public async Task SendTextMessageAsync(
        Enum enumLang,
        IReplyMarkup replyMarkup = null,
        int replyToMsgId = -1,
        ChatId customChatId = null,
        bool disableWebPreview = false,
        DateTime scheduleDeleteAt = default,
        bool includeSenderMessage = false,
        MessageFlag messageFlag = default,
        bool preventDuplicateSend = false
    )
    {
        var message = await GetLocalizationString(enumLang);

        await SendTextMessageAsync(
            message,
            replyMarkup,
            replyToMsgId,
            customChatId,
            disableWebPreview,
            scheduleDeleteAt,
            includeSenderMessage,
            messageFlag,
            preventDuplicateSend
        );
    }

    public async Task DeleteSenderMessageAsync()
    {
        var messageId = MessageOrEdited.MessageId;
        await DeleteAsync(messageId);
    }

    public async Task DeleteSentMessageAsync()
    {
        var messageId = SentMessage.MessageId;
        await DeleteAsync(messageId);
    }

    public async Task DeleteCurrentCallbackMessageAsync()
    {
        var messageId = CallbackMessage.MessageId;
        await DeleteAsync(messageId);
    }

    public async Task DeleteAsync(
        int messageId = -1,
        int delay = 0
    )
    {
        Thread.Sleep(delay);
        var targetMessageId = -1;

        try
        {
            targetMessageId = messageId != -1 ? messageId : SentMessage.MessageId;
            Log.Information(
                "Delete MsgId: {MsgId} on ChatId: {ChatId}",
                targetMessageId,
                ChatId
            );
            await Client.DeleteMessageAsync(ChatId, targetMessageId);
        }
        catch (Exception ex)
        {
            if (ex.IsErrorAsWarning())
            {
                Log.Warning(
                    ex,
                    "Error Delete MessageId {MessageId} On ChatId {ChatId}",
                    targetMessageId,
                    ChatId
                );
            }
            else
            {
                Log.Error(
                    ex,
                    "Error Delete MessageId {MessageId} On ChatId {ChatId}",
                    targetMessageId,
                    ChatId
                );
            }
        }
    }

    public async Task<Message> ForwardMessageAsync(
        int messageId = -1,
        long toChatId = -1
    )
    {
        if (toChatId == -1) toChatId = _eventLogConfig.ChannelId;
        if (messageId == -1) messageId = MessageOrEdited.MessageId;

        var forwardMessage = await Client.ForwardMessageAsync(
            toChatId,
            ChatId,
            messageId
        );

        return forwardMessage;
    }

    public async Task AnswerCallbackQueryAsync(
        string text,
        bool showAlert = false
    )
    {
        try
        {
            var callbackQueryId = CallbackQuery.Id;
            await Client.AnswerCallbackQueryAsync(
                callbackQueryId,
                text,
                showAlert
            );
        }
        catch (Exception e)
        {
            Log.Error(e, "Error Answer Callback");
        }
    }

    private void PreventDuplicateSend(
        bool preventDuplicateSend,
        MessageFlag flag,
        int skipLast = 1
    )
    {
        if (!preventDuplicateSend) return;

        var messageFlag = GetMessageFlag(flag);

        Log.Debug(
            "Preventing duplicate send Message at ChatId: {ChatId} with Flag: {MessageFlag}",
            ChatId,
            messageFlag
        );

        ChatService.DeleteMessageHistory(
                predicate: history =>
                    history.MessageFlag == messageFlag &&
                    history.ChatId == ChatId,
                skipLast: skipLast
            )
            .InBackground();
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

    public T GetCallbackDataAt<T>(int index)
    {
        dynamic messageId = CallbackQueryDatas.ElementAtOrDefault(index) as object;
        return Convert.ChangeType(messageId, typeof(T));
    }

    public T GetInlineQueryAt<T>(int index)
    {
        dynamic query = InlineQuery.Query.Split(" ").ElementAtOrDefault(index);

        return Convert.ChangeType(query, typeof(T));
    }

    #endregion Message

    #region Member

    public async Task<bool> KickMemberAsync(
        long userId,
        bool unban = false,
        DateTime untilDate = default
    )
    {
        bool isKicked;

        Log.Information(
            "Send request Kick chat member UserId: {UserId} at ChatId: {ChatId}",
            userId,
            ChatId
        );

        if (untilDate == default)
            // Under 30s and more than 366d is considered as forever
            untilDate = DateTime.Now.AddSeconds(10);

        try
        {
            await Client.BanChatMemberAsync(
                chatId: ChatId,
                userId: userId,
                untilDate: untilDate
            );

            if (unban) await UnBanMemberAsync(userId);
            isKicked = true;
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "Error request Kick chat Member UserId: {UserId} at ChatId: {ChatId}",
                userId,
                ChatId
            );
            isKicked = false;
        }

        return isKicked;
    }

    public async Task UnbanMemberAsync(User user = null)
    {
        var idTarget = user.Id;
        Log.Information(
            "Unban {IdTarget} from {ChatId}",
            idTarget,
            ChatId
        );

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
        Log.Information(
            "Unban {UserId} from {ChatId}",
            userId,
            ChatId
        );

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
                chatId: ChatId,
                userId: userId,
                canDeleteMessages: true,
                canManageVideoChats: true,
                canRestrictMembers: true,
                canPromoteMembers: true,
                canInviteUsers: true,
                canPinMessages: true
            );

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
            await Client.PromoteChatMemberAsync(ChatId, userId);

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

    public async Task<TelegramResult> RestrictMemberAsync(
        long userId,
        bool unMute = false,
        DateTime until = default
    )
    {
        var tgResult = new TelegramResult();

        try
        {
            var untilDate = until;
            if (until == default) untilDate = DateTime.UtcNow.AddDays(366);

            Log.Information(
                "Restricting UserId: '{UserId}'@'{ChatId}', UnMute: '{UnMute}' until {UntilDate}",
                userId,
                ChatId,
                unMute,
                untilDate
            );

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

            await Client.RestrictChatMemberAsync(
                ChatId,
                userId,
                permission,
                untilDate
            );

            tgResult.IsSuccess = true;

            Log.Debug(
                "MemberID {UserId} muted until {UntilDate}",
                userId,
                untilDate
            );
        }
        catch (Exception ex)
        {
            Log.Error(
                ex.Demystify(),
                "Error restrict userId: {UserId} on {ChatId}",
                userId,
                ChatId
            );
            var exceptionMsg = ex.Message;
            if (exceptionMsg.Contains("CHAT_ADMIN_REQUIRED")) Log.Debug("I'm must Admin on this Group!");

            tgResult.IsSuccess = false;
            tgResult.Exception = ex;
        }

        return tgResult;
    }

    public async Task<TelegramResult> UnmuteChatMemberAsync(long userId)
    {
        return await RestrictMemberAsync(userId, true);
    }

    public string[] GlobalIgnoreIds()
    {
        var ignoredIds = new[]
        {
            "777000"
        };

        return ignoredIds;
    }

    public bool IsGlobalIgnored(long userId = -1)
    {
        var ignoredIds = GlobalIgnoreIds();

        if (userId == -1) userId = FromId;

        return ignoredIds.Contains(userId.ToString());
    }

    public bool CheckUsername()
    {
        if (IsGlobalIgnored()) return false;

        if (From == null) return false;

        return From.Username == null;
    }

    #endregion Member

    #region OnUpdate

    private async Task<bool> CheckPermission()
    {
        if (IsPrivateChat) return true;

        if (!await CheckBotAdmin()) return true;

        if (await CheckFromAdmin()) return true;

        return false;
    }

    public async Task<bool> RunCheckUserUsername()
    {
        var op = Operation.Begin(
            "Check Username for UserId: '{UserId}' on ChatId: '{ChatId}'",
            FromId,
            ChatId
        );

        if (ChannelOrEditedPost != null)
        {
            op.Complete();
            return true;
        }

        if (HasUsername)
        {
            Log.Information("UserID '{FromId}' has set Username", FromId);
            op.Complete();

            return true;
        }

        if (await CheckPermission())
        {
            op.Complete();
            return true;
        }

        var chatSettings = await GetChatSetting();

        if (!chatSettings.EnableWarnUsername)
        {
            Log.Information("Warn Username is disabled on ChatID '{ChatId}'", ChatId);
            op.Complete();

            return true;
        }

        await SendWarningStep(StepHistoryName.ChatMemberUsername);
        op.Complete();

        return false;
    }

    public async Task<bool> RunCheckUserProfilePhoto()
    {
        try
        {
            var op = Operation.Begin(
                "Check Chat Photo on ChatId {ChatId} for UserId: {UserId}",
                ChatId,
                FromId
            );

            if (CallbackQuery != null ||
                ChannelOrEditedPost != null ||
                ChosenInlineResult != null ||
                ChatJoinRequest != null
               )
            {
                Log.Information("Check user profile photo skipped because Update type is '{UpdateType}'", Update.Type);
                op.Complete();
                return true;
            }

            if (await CheckPermission())
            {
                op.Complete();
                return true;
            }

            var hasProfilePhoto = await _userProfilePhotoService.CheckUserProfilePhoto(ChatId, FromId);

            if (hasProfilePhoto)
            {
                op.Complete();
                return true;
            }

            op.Complete();

            await SendWarningStep(StepHistoryName.ChatMemberPhoto);
            return false;
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "Error Check user profile photo at ChatId: {ChatId}, UserId: {UserId}",
                ChatId,
                FromId
            );

            return true;
        }
    }

    public async Task SendWarningStep(StepHistoryName name)
    {
        var humanSpan = KickTimeOffset.Humanize();
        var featureName = name.Humanize().Pascalize();
        var messageFlag = featureName.ToEnum(MessageFlag.General);

        var sendWarn = $"Hai {FromNameLink}, kamu belum mengatur {featureName}. silakan atur {featureName} yak. " +
                       $"Jika sudah atur {featureName}, silakan tekan tombol dibawah ini untuk verifikasi, " +
                       $"atau dalam <b>{humanSpan}</b>, Anda akan di tendang!";

        var verifyButton = new InlineKeyboardMarkup
        (
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Verifikasi", "verify")
            }
        );

        await RestrictMemberAsync(FromId, until: KickTimeOffset.ToDateTime());
        await SendTextMessageAsync(
            sendWarn,
            verifyButton,
            disableWebPreview: true,
            replyToMsgId: 0,
            scheduleDeleteAt: KickTimeOffset.ToDateTime().ToUniversalTime(),
            messageFlag: messageFlag,
            preventDuplicateSend: true
        );

        await ScheduleKickJob(name);
    }

    public async Task<StringAnalyzer> FireAnalyzer()
    {
        var settings = await GetChatSetting();
        StringAnalyzer result = new();

        if (!settings.EnableFireCheck)
        {
            Log.Information("Fire Check is disabled on ChatID '{ChatId}'", ChatId);
            return result;
        }

        result = ChatService.FireAnalyzer(MessageOrEditedText);

        if (!result.IsFired) return result;
        var muteUntil = result.FireRatio * 1.33;
        var untilDate = DateTime.Now.AddHours(muteUntil);

        var sendText = result.ResultNote;

        if (!await CheckUserPermission())
        {
            sendText += $"\nAnda di Mute sampai {untilDate} ";
            await RestrictMemberAsync(FromId, until: untilDate);
        }

        await SendTextMessageAsync(sendText);
        return result;
    }

    public async Task ScheduleKickJob(StepHistoryName name)
    {
        Log.Information("Scheduling Job with name: {JobName}", name);

        var jobId = _backgroundJob.Schedule<JobsService>(
            job =>
                job.MemberKickJob(ChatId, FromId),
            KickTimeOffset
        );

        await _stepHistoriesService.SaveStepHistory
        (
            new StepHistory
            {
                Name = name,
                ChatId = ChatId,
                UserId = FromId,
                FirstName = From.FirstName,
                LastName = From.LastName,
                Reason = $"User don't have {name.Humanize()}",
                Status = StepHistoryStatus.NeedVerify,
                HangfireJobId = jobId,
                LastWarnMessageId = SentMessage?.MessageId ?? -1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            }
        );
    }

    #endregion OnUpdate

    #region PostUpdate

    public async Task EnsureChatSettingsAsync()
    {
        if (InlineQuery != null)
        {
            Log.Debug("Ensure Chat Admin disabled because update type is {UpdateType}", Update.Type);
            return;
        }

        var op = Operation.Begin("Ensure Chat Settings for ChatId: '{ChatId}'", ChatId);

        try
        {
            var isBotAdmin = await CheckBotAdmin();
            var memberCount = await GetMemberCount();

            var saveSettings = -1;
            Dictionary<string, object> chatSettingsValues;
            var settings = await SettingsService.GetSettingsByGroupCore(ChatId);

            if (settings == null)
            {
                var chatSettingsFresh = new ChatSettingsInsertDto()
                {
                    ChatId = ChatId,
                    ChatTitle = ChatTitle,
                    ChatType = Chat.Type.Humanize(),
                    MembersCount = memberCount,
                    IsAdmin = isBotAdmin,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                chatSettingsValues = chatSettingsFresh.ToDictionary();
            }
            else
            {
                var chatSettingsEnsure = new ChatSettingsEnsureDto()
                {
                    ChatId = ChatId,
                    ChatTitle = ChatTitle,
                    ChatType = Chat.Type.Humanize(),
                    MembersCount = memberCount,
                    IsAdmin = isBotAdmin,
                    UpdatedAt = DateTime.Now
                };

                chatSettingsValues = chatSettingsEnsure.ToDictionary();
            }

            saveSettings = await SettingsService.SaveSettingsAsync(chatSettingsValues);

            Log.Debug(
                "Ensure Settings for ChatID: '{ChatId}' result {SaveSettings}",
                ChatId,
                saveSettings
            );

            op.Complete();
        }
        catch (Exception exception)
        {
            Log.Error(
                "Error when Ensure Chat Settings at '{ChatId}'. Error: {Exception}",
                ChatId,
                exception.Message
            );
        }
    }

    #endregion PostUpdate

    #region Message History

    public void SaveToMessageHistory(
        DateTime deleteAt = default,
        bool includeSender = false,
        MessageFlag flag = MessageFlag.General
    )
    {
        if (SentMessage == null) return;

        var messageFlag = GetMessageFlag(flag);

        var sentMessageId = SentMessage.MessageId;
        SaveMessageToHistoryAsync(
                sentMessageId,
                messageFlag,
                deleteAt
            )
            .InBackground();

        if (!includeSender) return;

        var senderMessageId = MessageOrEdited.MessageId;
        SaveMessageToHistoryAsync(
                senderMessageId,
                messageFlag,
                deleteAt
            )
            .InBackground();
    }

    public void SaveSenderMessageToHistory(
        MessageFlag messageFlag,
        DateTime deleteAt = default
    )
    {
        if (MessageOrEdited == null) return;

        var messageId = MessageOrEdited.MessageId;
        SaveMessageToHistoryAsync(
                messageId,
                messageFlag,
                deleteAt
            )
            .InBackground();
    }

    public void SaveSentMessageToHistory(
        MessageFlag messageFlag,
        DateTime deleteAt = default
    )
    {
        if (SentMessage == null) return;

        var messageId = SentMessage.MessageId;
        SaveMessageToHistoryAsync(
                messageId,
                messageFlag,
                deleteAt
            )
            .InBackground();
    }

    public async Task SaveMessageToHistoryAsync(
        long messageId,
        MessageFlag messageFlag,
        DateTime deleteAt = default
    )
    {
        var messageFlagStr = messageFlag.Humanize().Pascalize();
        if (deleteAt == default) deleteAt = DateTime.UtcNow.AddMinutes(1);

        var saveHistory = await MessageHistoryService.SaveToMessageHistoryAsync(
            new MessageHistoryInsertDto()
            {
                MessageFlag = messageFlagStr,
                FromId = FromId,
                ChatId = ChatId,
                MessageId = messageId,
                DeleteAt = deleteAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        Log.Information(
            "Save To Message History for {MessageId} at ChatId: {ChatId} with Flag: {MessageFlag} Result: {SaveHistory}",
            messageId,
            ChatId,
            messageFlagStr,
            saveHistory
        );
    }

    #endregion Message History
}
