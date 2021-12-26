using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Options;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.Services.Telegram;

public class ChatService
{
    private const string AdminCheckerPrefix = "admin-checker";
    private const int PrivateSettingLimit = 365;

    private readonly TelegramBotClient _botClient;
    private readonly SettingsService _settingsService;
    private readonly CacheService _cacheService;
    private readonly RestrictionConfig _restrictionConfig;
    private readonly CacheConfig _cacheConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatService"/> class
    /// </summary>
    /// <param name="cacheService"></param>
    /// <param name="cachingConfig">The caching config</param>
    /// <param name="restrictionConfig">The</param>
    /// <param name="botClient">The bot client</param>
    /// <param name="settingsService">The settings service</param>
    public ChatService(CacheService cacheService,
        IOptionsSnapshot<CacheConfig> cachingConfig,
        IOptionsSnapshot<RestrictionConfig> restrictionConfig,
        TelegramBotClient botClient,
        SettingsService settingsService
    )
    {
        _botClient = botClient;
        _cacheService = cacheService;
        _restrictionConfig = restrictionConfig.Value;
        _cacheConfig = cachingConfig.Value;
        _settingsService = settingsService;
    }

    /// <summary>
    /// Gets the chat using the specified chat id
    /// </summary>
    /// <param name="chatId">The chat id</param>
    /// <returns>The data</returns>
    public async Task<Chat> GetChatAsync(long chatId)
    {
        var cacheKey = "chat_" + chatId.ReduceChatId();
        var data = await _cacheService.GetOrSetAsync(cacheKey, async () => {
            var chat = await _botClient.GetChatAsync(chatId);

            return chat;
        });

        return data;
    }

    public async Task<long> GetMemberCountAsync(long chatId)
    {
        var reducedChatId = chatId.ReduceChatId();
        var cacheKey = $"member-count_{reducedChatId}";

        var getMemberCount = await _cacheService.GetOrSetAsync(cacheKey, async () => {
            var memberCount = await _botClient.GetChatMemberCountAsync(chatId);

            return memberCount;
        });

        return getMemberCount;
    }

    public async Task<ChatMember> GetChatMemberAsync(long chatId, long userId)
    {
        var cacheKey = "chat-member_" + chatId.ReduceChatId() + $"_{userId}";
        var data = await _cacheService.GetOrSetAsync(cacheKey, async () => {
            var chat = await _botClient.GetChatMemberAsync(chatId, userId);

            return chat;
        });

        return data;
    }

    public async Task<UserProfilePhotos> GetChatPhotoAsync(long userId)
    {
        var (expireAfter, staleAfter) = _cacheConfig;
        var cacheKey = "user-profile-photos_-" + userId;

        var data = await _cacheService.GetOrSetAsync(cacheKey, async () => {
            var userProfilePhotos = await _botClient.GetUserProfilePhotosAsync(userId);

            return userProfilePhotos;
        });

        return data;
    }

    /// <summary>
    /// The is private chat
    /// </summary>
    public async Task<bool> IsPrivateChat(long chatId)
    {
        var chat = await GetChatAsync(chatId);
        var isPrivateChat = chat.Type == ChatType.Private;

        return isPrivateChat;
    }

    public async Task<bool> CheckGroupRestriction(long chatId)
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

    /// <summary>
    /// Registers the chat health
    /// </summary>
    public async Task RegisterChatHealth()
    {
        Log.Information("Starting Check bot is Admin on all Group!");

        await _settingsService.PurgeSettings(PrivateSettingLimit);

        var allSettings = await _settingsService.GetAllSettings();

        Parallel.ForEach(allSettings, (chatSetting, state, args) => {
            var chatId = chatSetting.ChatId.ToInt64();
            var reducedChatId = chatId.ReduceChatId();

            var adminCheckerId = AdminCheckerPrefix + "-" + reducedChatId;

            if (chatSetting.ChatType != ChatTypeEx.Private)
            {
                Log.Debug("Creating Chat Jobs for ChatID '{ChatId}'", chatId);
                HangfireUtil.RegisterJob(adminCheckerId, (PrivilegeService service) =>
                    service.AdminCheckerJobAsync(chatId), Cron.Daily, queue: "admin-checker", fireAfterRegister: false);
            }
            else
            {
                var dateNow = DateTime.UtcNow;
                var diffDays = (dateNow - chatSetting.UpdatedAt).TotalDays;
                Log.Debug("Last activity days in '{ChatId}' is {DiffDays:N2} days", chatId, diffDays);
            }
        });
    }

    // /// <summary>
    // /// Chats the clean up
    // /// </summary>
    // [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    // public async Task ChatCleanUp()
    // {
    //     Log.Information("Starting Check bot is Admin on all Group!");
    //
    //     var allSettings = await _settingsService.GetAllSettings();
    //
    //     foreach (var chatGroup in allSettings)
    //     {
    //         var chatId = chatGroup.ChatId.ToInt64();
    //
    //         try
    //         {
    //             await privilegeService.AdminCheckerJobAsync(chatId);
    //         }
    //         catch (Exception ex)
    //         {
    //             var msgEx = ex.Message.ToLower();
    //
    //             if (msgEx.Contains("bot is not a member"))
    //             {
    //                 Log.Warning("This bot may has leave from this chatId '{ChatId}'", chatId);
    //             }
    //             else
    //             {
    //                 Log.Error(ex.Demystify(), "Error when checking ChatID: {ChatId}", chatId);
    //             }
    //         }
    //     }
    // }
}