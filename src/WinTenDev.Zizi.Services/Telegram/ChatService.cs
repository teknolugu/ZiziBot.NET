using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Telegram;

public class ChatService
{
    private const string AdminCheckerPrefix = "admin-checker";
    private const int PrivateSettingLimit = 365;

    private readonly ILogger<ChatService> _logger;
    private readonly ITelegramBotClient _botClient;
    private readonly SettingsService _settingsService;
    private readonly CacheService _cacheService;
    private readonly RestrictionConfig _restrictionConfig;

    public ChatService(
        ILogger<ChatService> logger,
        CacheService cacheService,
        IOptionsSnapshot<RestrictionConfig> restrictionConfig,
        ITelegramBotClient botClient,
        SettingsService settingsService
    )
    {
        _logger = logger;
        _botClient = botClient;
        _cacheService = cacheService;
        _restrictionConfig = restrictionConfig.Value;
        _settingsService = settingsService;
    }

    public bool IsEnableRestriction()
    {
        var isRestricted = _restrictionConfig.EnableRestriction;
        Log.Debug("Global Restriction IsEnabled: {IsRestricted}", isRestricted);

        return isRestricted;
    }

    public bool CheckChatRestriction(long chatId)
    {
        try
        {
            var isRestricted = false;

            if (!IsEnableRestriction()) return false;

            var restrictArea = _restrictionConfig.RestrictionArea;

            if (restrictArea != null)
            {
                var match = restrictArea.FirstOrDefault(x => x == chatId.ToString());

                if (match == null) isRestricted = true;
            }

            Log.Information("Check Chat restriction result on ChatId: {ChatId}? IsRestricted: {IsRestricted}", chatId, isRestricted);
            return isRestricted;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error when check Chat Restriction on {ChatId}", chatId);
            return false;
        }
    }

    public bool CheckUserIdIgnored(long userId)
    {
        try
        {
            var ignored = false;
            var ignoredIds = _restrictionConfig.IgnoredIds;
            if (ignoredIds == null) return false;

            var find = ignoredIds.FirstOrDefault(l => l == userId);
            if (find != 0) ignored = true;

            Log.Information("Check UserId {UserId} is ignored at Global Ignore ID? {Ignored}", userId, ignored);

            return ignored;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error when check Ignore for UserId {UserId}", userId);
            return false;
        }
    }

    public async Task<Chat> GetChatAsync(long chatId)
    {
        var cacheKey = "chat_" + chatId.ReduceChatId();

        var data = await _cacheService.GetOrSetAsync
        (
            cacheKey, async () => {
                var chat = await _botClient.GetChatAsync(chatId);

                return chat;
            }
        );

        return data;
    }

    public async Task<long> GetMemberCountAsync(long chatId)
    {
        var reducedChatId = chatId.ReduceChatId();
        var cacheKey = $"member-count_{reducedChatId}";

        var getMemberCount = await _cacheService.GetOrSetAsync
        (
            cacheKey, async () => {
                var memberCount = await _botClient.GetChatMemberCountAsync(chatId);

                return memberCount;
            }
        );

        return getMemberCount;
    }

    public async Task<ChatMember> GetChatMemberAsync(
        long chatId,
        long userId
    )
    {
        var cacheKey = "chat-member_" + chatId.ReduceChatId() + $"_{userId}";

        var data = await _cacheService.GetOrSetAsync
        (
            cacheKey, async () => {
                var chat = await _botClient.GetChatMemberAsync(chatId, userId);

                return chat;
            }
        );

        return data;
    }

    public async Task<bool> IsPrivateChat(long chatId)
    {
        var chat = await GetChatAsync(chatId);
        var isPrivateChat = chat.Type == ChatType.Private;

        return isPrivateChat;
    }

    public bool CheckGroupRestriction(long chatId)
    {
        Log.Information("Checking Chat Restriction for {ChatId}", chatId);

        if (!_restrictionConfig.EnableRestriction)
        {
            Log.Debug("Chat Restriction is not enabled in this chat!");
            return false;
        }

        var checkRestricted = !_restrictionConfig.RestrictionArea.Contains(chatId.ToString());
        Log.Debug("Is ChatId {ChatId} restricted? {Check}", chatId, checkRestricted);

        return checkRestricted;
    }

    public StringAnalyzer FireAnalyzer(string text)
    {
        StringAnalyzer result = new();

        if (text == null)
        {
            _logger.LogDebug("No Message Text/Caption detected");

            return result;
        }

        result = text.AnalyzeString();
        var fireRatio = result.FireRatio;
        var wordCount = result.WordsCount;

        if (wordCount < 3)
        {
            _logger.LogDebug("String analyzer stop, because Words count is less than 3");
            return result;
        }

        var resultNote = fireRatio switch
        {
            >= 1 => "Tolong matikan CAPS LOCK sebelum mengetik pesan.",
            >= 0.6 => "Tolong kurangi penggunaan huruf kapital yang berlebihan.",
            _ => ""
        };

        result.ResultNote = resultNote;
        result.IsFired = fireRatio >= 0.6;

        return result;
    }

    public async Task RegisterChatHealth()
    {
        Log.Information("Starting Check bot is Admin on all Group!");

        await _settingsService.PurgeSettings(PrivateSettingLimit);

        var allSettings = await _settingsService.GetAllSettings();

        Parallel.ForEach
        (
            allSettings, (
                chatSetting,
                state,
                args
            ) => {
                var chatId = chatSetting.ChatId.ToInt64();
                var reducedChatId = chatId.ReduceChatId();

                var adminCheckerId = AdminCheckerPrefix + "-" + reducedChatId;

                if (chatSetting.ChatType != ChatTypeEx.Private)
                {
                    Log.Debug("Creating Chat Jobs for ChatID '{ChatId}'", chatId);

                    HangfireUtil.RegisterJob
                    (
                        adminCheckerId, (PrivilegeService service) =>
                            service.AdminCheckerJobAsync(chatId), Cron.Daily, queue: "admin-checker", fireAfterRegister: false
                    );
                }
                else
                {
                    var dateNow = DateTime.UtcNow;
                    var diffDays = (dateNow - chatSetting.UpdatedAt).TotalDays;
                    Log.Debug("Last activity days in '{ChatId}' is {DiffDays:N2} days", chatId, diffDays);
                }
            }
        );
    }
}