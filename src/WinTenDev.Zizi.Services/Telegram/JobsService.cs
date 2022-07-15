using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoreLinq;
using RepoDb;
using Serilog;
using SerilogTimings;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace WinTenDev.Zizi.Services.Telegram;

public class JobsService
{
    private readonly EventLogConfig _eventLogConfig;
    private readonly RestrictionConfig _restrictionConfig;
    private readonly ILogger<JobsService> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly StepHistoriesService _stepHistoriesService;
    private readonly ChatService _chatService;
    private readonly ITelegramBotClient _botClient;
    private readonly SettingsService _settingsService;

    public JobsService(
        ILogger<JobsService> logger,
        IOptionsSnapshot<EventLogConfig> eventLogConfig,
        IOptions<RestrictionConfig> restrictionConfig,
        IBackgroundJobClient backgroundJobClient,
        IRecurringJobManager recurringJobManager,
        StepHistoriesService stepHistoriesService,
        ChatService chatService,
        ITelegramBotClient botClient,
        SettingsService settingsService
    )
    {
        _eventLogConfig = eventLogConfig.Value;
        _restrictionConfig = restrictionConfig.Value;
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
        _stepHistoriesService = stepHistoriesService;
        _chatService = chatService;
        _botClient = botClient;
        _settingsService = settingsService;
    }

    [JobDisplayName("Member Kick Job {1}@{0}")]
    [AutomaticRetry(
        Attempts = 1,
        OnAttemptsExceeded = AttemptsExceededAction.Delete
    )]
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

        Log.Information(
            "Starting kick member for UserId {UserId} from {ChatId}",
            userId,
            chatId
        );

        var history = needVerify.FirstOrDefault();

        if (history == null) return;

        await _botClient.BanChatMemberAsync(chatId, userId);
        await _botClient.UnbanChatMemberAsync(chatId, userId);
        if (history.LastWarnMessageId != -1)
            await _botClient.DeleteMessageAsync(chatId, history.LastWarnMessageId);

        await UpdateStepHistoryStatus(chatId, userId);

        var chat = await _chatService.GetChatAsync(chatId);

        var sb = new StringBuilder()
            .Append("<b>Action:</b> #KickChatMember")
            .AppendLine()
            .Append("<b>User:</b> ")
            .AppendFormat("{0}\n", history.UserId.GetNameLink(history.FirstName, history.LastName))
            .Append("<b>Chat:</b> ")
            .AppendFormat("{0} \n", chat.GetChatNameLink())
            .Append("<b>Reason:</b> ")
            .AppendJoin(",", needVerify.Select(x => $"#{x.Name}"))
            .AppendLine()
            .AppendFormat(
                "#U{0} #C{1}",
                userId,
                chatId.ReduceChatId()
            );

        await _botClient.SendTextMessageAsync(
            text: sb.ToTrimmedString(),
            disableWebPagePreview: true,
            chatId: _eventLogConfig.ChannelId,
            parseMode: ParseMode.Html
        );

        Log.Information(
            "UserId {UserId} successfully kicked from {ChatId}",
            userId,
            chatId
        );
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
                        _recurringJobManager.AddOrUpdate<PrivilegeService>(
                            chatId.GetChatKey("AC"),
                            (x) => x.AdminCheckerJobAsync(chatId),
                            Cron.Daily,
                            queue: "admin-checker"
                        );
                        break;

                    default:
                        Log.Verbose(
                            "Currently no action for type {ChatType}",
                            chatSetting.ChatType
                        );
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex.Demystify(),
                    "Error when checking ChatID: {ChatId}",
                    chatId
                );
            }
        }
    }

    public void RegisterJobClearLog()
    {
        _recurringJobManager.AddOrUpdate<StorageService>(
            "log-cleaner",
            (service) => service.ClearLog(),
            Cron.Daily
        );
    }

    public void RegisterJobDeleteOldStep()
    {
        _recurringJobManager.AddOrUpdate<StepHistoriesService>(
            "delete-old-steps",
            service => service.DeleteOldStepHistory(),
            Cron.Daily
        );
    }

    public void RegisterJobDeleteOldRssHistory()
    {
        _recurringJobManager.AddOrUpdate<RssService>(
            "delete-old-rss-history",
            service => service.DeleteOldHistory(),
            Cron.Daily
        );
    }

    public void RegisterJobDeleteOldMessageHistory()
    {
        _recurringJobManager.AddOrUpdate<ChatService>(
            "clear-message-history",
            service => service.DeleteOldMessageHistoryAsync(),
            Cron.Minutely
        );
    }

    public void RegisterJobRunMysqlBackup()
    {
        _recurringJobManager.AddOrUpdate<DatabaseService>(
            "mysql-backup",
            service => service.AutomaticMysqlBackup(),
            Cron.Daily
        );
    }

    public void RegisterJobRunDeleteOldUpdates()
    {
        _recurringJobManager.AddOrUpdate<BotUpdateService>(
            "delete-old-updates",
            service => service.DeleteOldUpdateAsync(),
            Cron.Daily
        );
    }

    public void RegisterJobAdminCleanUp()
    {
        var adminCleanUp = _restrictionConfig.AdminCleanUp;

        if (adminCleanUp == null) return;

        var filteredCleanUp = adminCleanUp
            .Where(targetId => !targetId.Contains('_'))
            .ToList();

        if (!filteredCleanUp.Any())
        {
            Log.Information("No Admin CleanUp for register");
            return;
        }

        Log.Information("Starting register Admin CleanUp");

        filteredCleanUp.ForEach(
            targetId => {
                var chatId = targetId.ToInt64();
                var jobId = chatId.GetChatKey("admin-cleanup");

                Log.Debug(
                    "Register Admin Clean Up for ChatId: {ChatId} with JobId: {JobId}",
                    chatId,
                    jobId
                );

                _recurringJobManager.AddOrUpdate<PrivilegeService>(
                    recurringJobId: jobId,
                    methodCall: services => services.AdminCleanupAsync(chatId),
                    cronExpression: Cron.Daily
                );
            }
        );
    }

    public void TriggerJobsByPrefix(string prefixId)
    {
        var op = Operation.Begin("Trigger Jobs by Prefix. Prefix: {PrefixId}", prefixId);

        Log.Information("Loading Hangfire jobs..");
        var connection = JobStorage.Current.GetConnection();

        var recurringJobs = connection.GetRecurringJobs();
        var filteredJobs = recurringJobs.Where(dto => dto.Id.StartsWith(prefixId)).ToList();
        Log.Debug(
            "Found {Count} of {Count1}",
            filteredJobs.Count,
            recurringJobs.Count
        );

        var numOfJobs = filteredJobs.Count;

        Parallel.ForEach(
            filteredJobs,
            (
                recurringJobDto,
                parallelLoopState,
                index
            ) => {
                var recurringJobId = recurringJobDto.Id;

                Log.Debug(
                    "Triggering jobId: {RecurringJobId}, Index: {Index}",
                    recurringJobId,
                    index
                );

                _recurringJobManager.Trigger(recurringJobId);

                Log.Debug(
                    "Trigger succeeded {RecurringJobId}, Index: {Index}",
                    recurringJobId,
                    index
                );
            }
        );

        Log.Information(
            "Hangfire jobs successfully trigger. Total: {NumOfJobs}",
            numOfJobs
        );

        op.Complete();
    }

    public List<RecurringJobDto> GetRecurringJobs()
    {
        var connection = JobStorage.Current.GetConnection();
        var recurringJobs = connection.GetRecurringJobs();

        return recurringJobs;
    }

    public RecurringJobDto GetRecurringJobById(string jobId)
    {
        var recurringJobs = GetRecurringJobs();

        return recurringJobs.FirstOrDefault(dto => dto.Id == jobId);
    }

    [AutomaticRetry(
        Attempts = 2,
        LogEvents = true,
        OnAttemptsExceeded = AttemptsExceededAction.Delete
    )]
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

        var result = await _stepHistoriesService.UpdateStepHistoryStatus(
            new StepHistory
            {
                ChatId = chatId,
                UserId = userId,
                Status = StepHistoryStatus.ActionDone,
                UpdatedAt = DateTime.Now
            },
            fields
        );

        return result;
    }

    public void ClearPendingJobs()
    {
        _logger.LogInformation("Starting clear pending Jobs..");

        var monitor = JobStorage.Current.GetMonitoringApi();
        var queues = monitor.Queues();

        _logger.LogInformation("Found {QueueCount} queues to delete", queues.Count);

        queues.ForEach(
            dto =>
                dto.FirstJobs.ForEach(
                    job => _backgroundJobClient.Delete(job.Key)
                )
        );

        _logger.LogInformation("Clear pending Jobs done!");

    }
}