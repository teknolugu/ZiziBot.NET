using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Options;
using RepoDb;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Telegram;

public class JobsService
{
    private readonly EventLogConfig _eventLogConfig;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly StepHistoriesService _stepHistoriesService;
    private readonly ChatService _chatService;
    private readonly TelegramBotClient _botClient;
    private readonly SettingsService _settingsService;

    public JobsService(
        IOptionsSnapshot<EventLogConfig> eventLogConfig,
        IRecurringJobManager recurringJobManager,
        StepHistoriesService stepHistoriesService,
        ChatService chatService,
        TelegramBotClient botClient,
        SettingsService settingsService
    )
    {
        _eventLogConfig = eventLogConfig.Value;
        _recurringJobManager = recurringJobManager;
        _stepHistoriesService = stepHistoriesService;
        _chatService = chatService;
        _botClient = botClient;
        _settingsService = settingsService;
    }

    [JobDisplayName("Member Kick Job {1}@{0}")]
    [AutomaticRetry(Attempts = 1, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task MemberKickJob(
        long chatId,
        long userId
    )
    {
        var needVerify = await _stepHistoriesService.GetStepHistoryVerifyCore
        (
            new StepHistory()
            {
                ChatId = chatId,
                UserId = userId
            }
        );

        if (!needVerify.Any()) return;

        Log.Information("Starting kick member for UserId {UserId} from {ChatId}", userId, chatId);

        var history = needVerify.FirstOrDefault();

        if (history == null) return;

        await _botClient.BanChatMemberAsync(chatId, userId);
        await _botClient.UnbanChatMemberAsync(chatId, userId);
        if (history.LastWarnMessageId != -1)
            await _botClient.DeleteMessageAsync(chatId, history.LastWarnMessageId);

        await UpdateStepHistoryStatus(chatId, userId);

        var chat = await _chatService.GetChatAsync(chatId);

        var sb = new StringBuilder()
            .Append("<b>Action:</b> #KickChatMember").AppendLine()
            .Append("<b>User:</b> ").AppendFormat("{0}\n", history.UserId.GetNameLink(history.FirstName, history.LastName))
            .Append("<b>Chat:</b> ").AppendFormat("{0} \n", chat.Username.GetChatNameLink(chat.Title))
            .Append("<b>Reason:</b> ").AppendJoin(",", needVerify.Select(x => $"#{x.Name}")).AppendLine()
            .AppendFormat("#U{0} #C{1}", userId, chatId.ReduceChatId());

        await _botClient.SendTextMessageAsync
        (
            text: sb.ToTrimmedString(),
            disableWebPagePreview: true,
            chatId: _eventLogConfig.ChannelId,
            parseMode: ParseMode.Html
        );

        Log.Information("UserId {UserId} successfully kicked from {ChatId}", userId, chatId);
    }

    public async Task RegisterJobChatCleanUp()
    {
        Log.Information("Starting Check bot is Admin on all Group!");

        var allSettings = await _settingsService.GetAllSettings();

        foreach (var chatSetting in allSettings)
        {
            var chatId = chatSetting.ChatId;

            try
            {
                switch (chatSetting.ChatType)
                {
                    case ChatTypeEx.Group:
                    case ChatTypeEx.SuperGroup:
                        _recurringJobManager.AddOrUpdate<PrivilegeService>
                        (
                            chatId.GetChatKey("AC"),
                            (x) => x.AdminCheckerJobAsync(chatId), Cron.Daily, queue: "admin-checker"
                        );
                        break;
                    default:
                        Log.Verbose("Currently no action for type {ChatType}", chatSetting.ChatType);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "Error when checking ChatID: {ChatId}", chatId);
            }
        }
    }

    public void RegisterJobClearLog()
    {
        _recurringJobManager.AddOrUpdate<StorageService>
        (
            "log-cleaner",
            (service) => service.ClearLog(), Cron.Daily
        );
    }

    public void RegisterJobDeleteOldStep()
    {
        _recurringJobManager.AddOrUpdate<StepHistoriesService>
        (
            "delete-old-steps",
            service => service.DeleteOldStepHistory(), Cron.Daily
        );
    }

    public void RegisterJobDeleteOldRssHistory()
    {
        _recurringJobManager.AddOrUpdate<RssService>
        (
            "delete-old-rss-history",
            service => service.DeleteOldHistory(), Cron.Daily
        );
    }

    [AutomaticRetry(Attempts = 2, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task SendEventLog(
        long chatId,
        string messageText
    )
    {
        var chatIds = new List<long>()
        {
            chatId,
            _eventLogConfig.ChannelId
        };

        foreach (var targetChatId in chatIds)
        {
            await _botClient.SendTextMessageAsync(targetChatId, messageText);
        }
    }

    public async Task<int> UpdateStepHistoryStatus(
        long chatId,
        long userId
    )
    {
        var fields = Field.Parse<StepHistory>
        (
            history => new
            {
                history.Status,
                history.UpdatedAt
            }
        );

        var result = await _stepHistoriesService.UpdateStepHistoryStatus
        (
            new StepHistory
            {
                ChatId = chatId,
                UserId = userId,
                Status = StepHistoryStatus.ActionDone,
                UpdatedAt = DateTime.Now
            }, fields
        );

        return result;
    }
}