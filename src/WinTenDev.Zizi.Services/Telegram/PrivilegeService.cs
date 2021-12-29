using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.Services.Telegram;

/// <summary>
/// This class have an about Privilege tools
/// </summary>
public class PrivilegeService
{
    private const int AfterLeaveLimit = 30;
    private readonly ILogger<PrivilegeService> _logger;
    private readonly CacheService _cacheService;
    private readonly BotService _botService;
    private readonly ChatService _chatService;
    private readonly SettingsService _settingsService;
    private readonly TelegramBotClient _botClient;
    private readonly RestrictionConfig _restrictionConfig;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="restrictionConfig"></param>
    /// <param name="logger"></param>
    /// <param name="cacheService"></param>
    /// <param name="botService"></param>
    /// <param name="chatService"></param>
    /// <param name="settingsService"></param>
    /// <param name="botClient"></param>
    public PrivilegeService(
        IOptionsSnapshot<RestrictionConfig> restrictionConfig,
        ILogger<PrivilegeService> logger,
        CacheService cacheService,
        BotService botService,
        ChatService chatService,
        SettingsService settingsService,
        TelegramBotClient botClient
    )
    {
        _logger = logger;
        _cacheService = cacheService;
        _botService = botService;
        _chatService = chatService;
        _settingsService = settingsService;
        _botClient = botClient;
        _restrictionConfig = restrictionConfig.Value;
    }

    /// <summary>
    /// If given UserId is Sudoers, method will be return true
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public bool IsFromSudo(long userId)
    {
        bool isSudo = false;
        var sudoers = _restrictionConfig.Sudoers;
        if (sudoers != null)
        {
            isSudo = sudoers.Contains(userId.ToString());
        }

        Log.Debug("Is UserId '{UserId}' Sudo? '{IsSudo}'", userId, isSudo);

        return isSudo;
    }

    /// <summary>
    /// Get of list Administrators in ChatId
    /// </summary>
    /// <param name="chatId"></param>
    /// <returns></returns>
    public async Task<ChatMember[]> GetChatAdministratorsAsync(long chatId)
    {
        var cacheKey = "chat-admin_" + chatId.ReduceChatId();
        var administrators = await _cacheService.GetOrSetAsync(cacheKey, async () =>
            await _botClient.GetChatAdministratorsAsync(chatId));

        return administrators;
    }

    /// <summary>
    /// If given UserId is admin in ChatId, method will be return true
    /// </summary>
    /// <param name="chatId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<bool> IsAdminAsync(long chatId, long userId)
    {
        var listAdmins = await GetChatAdministratorsAsync(chatId);
        var isAdmin = listAdmins.Any(member => member.User.Id == userId);
        _logger.LogDebug("Check UserId '{UserId}' IsAdmin? '{IsAdmin}'", userId, isAdmin);

        return isAdmin;
    }

    public async Task<bool> IsBotAdminAsync(long chatId)
    {
        var getMe = await _botService.GetMeAsync();

        var isBotAdmin = await IsAdminAsync(chatId, getMe.Id);

        return isBotAdmin;
    }

    public async Task<bool> IsAdminOrPrivateChat(long chatId, long userId)
    {
        var isPrivate = await _chatService.IsPrivateChat(chatId);
        if (isPrivate) return true;

        var isAdmin = await IsAdminAsync(chatId, userId);
        return isAdmin;
    }

    public bool IsFromAnonymous(long userId)
    {
        var from777K = 7770000 == userId;

        return from777K;
    }

    /// <summary>
    /// Admins the checker job using the specified chat id
    /// </summary>
    /// <param name="chatId">The chat id</param>
    [AutomaticRetry(Attempts = 1, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task AdminCheckerJobAsync(long chatId)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            Log.Information("Starting check Admin in ChatID '{ChatId}'", chatId);

            var me = await _botClient.GetMeAsync();
            var isAdminChat = await this.IsAdminAsync(chatId, me.Id);

            if (isAdminChat)
            {
                await Task.Delay(5000);
                return;
            }

            Log.Debug("Doing leave chat from {ChatId}", chatId);
            var msgLeave = "Sepertinya saya bukan admin di grup ini, saya akan meninggalkan grup. Sampai jumpa!" +
                           "\n\nTerima kasih sudah menggunakan @MissZiziBot, silakan undang saya kembali jika diperlukan.";

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithUrl("👥 Dukungan Grup", "https://t.me/WinTenDev")
                    // InlineKeyboardButton.WithUrl("↖️ Tambahkan ke Grup", urlAddTo)
                }
            });

            await _botClient.SendTextMessageAsync(chatId, msgLeave, ParseMode.Html, replyMarkup: inlineKeyboard);
            await _botClient.LeaveChatAsync(chatId);

            Log.Debug("Checking Admin in ChatID '{ChatId}' job complete in {Elapsed}", chatId, sw.Elapsed);
            sw.Stop();

            await Task.Delay(5000);
        }
        catch (ApiRequestException requestException)
        {
            Log.Error(requestException, "Error API Request when Check Admin in ChatID: '{ChatId}'", chatId);
            var setting = await _settingsService.GetSettingsByGroupCore(chatId);

            var exMessage = requestException.Message.ToLower();
            if (exMessage.IsContains("forbidden"))
            {
                var dateNow = DateTime.UtcNow;
                var diffDays = (dateNow - setting.UpdatedAt).TotalDays;
                Log.Debug("Last activity days in '{ChatId}' is {DiffDays:N2} days", chatId, diffDays);

                if (diffDays > AfterLeaveLimit)
                {
                    Log.Debug("This time is cleanup this chat!");
                    await _settingsService.DeleteSettings(chatId);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error when Check Admin in ChatID: '{ChatId}'", chatId);
            throw;
        }
    }
}