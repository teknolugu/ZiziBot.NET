using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoreLinq;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

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
    private readonly ITelegramBotClient _botClient;
    private readonly WTelegramApiService _wTelegramApiService;
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
        ITelegramBotClient botClient,
        WTelegramApiService wTelegramApiService
    )
    {
        _logger = logger;
        _cacheService = cacheService;
        _botService = botService;
        _chatService = chatService;
        _settingsService = settingsService;
        _botClient = botClient;
        _wTelegramApiService = wTelegramApiService;
        _restrictionConfig = restrictionConfig.Value;
    }

    /// <summary>
    /// If given UserId is Sudoers, method will be return true
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public bool IsFromSudo(long userId)
    {
        var isSudo = false;
        var sudoers = _restrictionConfig.Sudoers;

        if (sudoers != null)
        {
            isSudo = sudoers.Contains(userId.ToString());
        }

        Log.Debug(
            "Is UserId '{UserId}' Sudo? '{IsSudo}'",
            userId,
            isSudo
        );

        return isSudo;
    }

    /// <summary>
    /// Get of list Administrators in ChatId
    /// </summary>
    /// <param name="chatId"></param>
    /// <returns></returns>
    public async Task<ChatMember[]> GetChatAdministratorsAsync(long chatId)
    {
        var administrators = await _cacheService.GetOrSetAsync(
            cacheKey: "chat-admin_" + chatId.ReduceChatId(),
            action: async () =>
                await _botClient.GetChatAdministratorsAsync(chatId)
        );

        return administrators;
    }

    public async Task<ChannelParticipants> GetChatAdministratorsTgApiAsync(long chatId)
    {
        var adminParticipants = await _cacheService.GetOrSetAsync(
            cacheKey: "admin-tg-api_" + chatId.ReduceChatId(),
            action: async () => {
                var adminParticipants = await _wTelegramApiService.GetChatAdministratorsCore(chatId);

                return adminParticipants;
            }
        );

        return adminParticipants;
    }

    /// <summary>
    /// If given UserId is admin in ChatId, method will be return true
    /// </summary>
    /// <param name="chatId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<bool> IsAdminAsync(
        long chatId,
        long userId
    )
    {
        var listAdmins = await GetChatAdministratorsAsync(chatId);
        var isAdmin = listAdmins.Any(member => member.User.Id == userId);
        _logger.LogDebug(
            "Check UserId '{UserId}' IsAdmin? '{IsAdmin}'",
            userId,
            isAdmin
        );

        return isAdmin;
    }

    public async Task<bool> IsBotAdminAsync(long chatId)
    {
        var getMe = await _botService.GetMeAsync();

        var isBotAdmin = await IsAdminAsync(chatId, getMe.Id);

        return isBotAdmin;
    }

    [Obsolete("Please use separated method IsAdminAsync() and property IsPrivateChat instead of this method.")]
    public async Task<bool> IsAdminOrPrivateChat(
        long chatId,
        long userId
    )
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

    [JobDisplayName("Admin CleanUp {0}")]
    public async Task AdminCleanupAsync(long chatId)
    {
        var admins = await _botClient.GetChatAdministratorsAsync(chatId);
        var demoted = new List<ChatMember>();

        await admins.ForEachAsync(
            6,
            async chatMember => {
                var userId = chatMember.User.Id;

                Log.Debug(
                    "Demoting UserId: {UserId} at ChatId: {ChatId}",
                    userId,
                    chatId
                );

                try
                {
                    await _botClient.PromoteChatMemberAsync(chatId, userId);

                    demoted.Add(chatMember);
                }
                catch (Exception exception)
                {
                    Log.Warning(
                        "Error when Demoting UserId: {UserId}. Exception: {Message}",
                        userId,
                        exception.Message
                    );
                }
            }
        );

        var messageBuilder = new StringBuilder();
        messageBuilder.Append("Jadwal ganti petugas, silakan /promote kembali jika di perlukan")
            .AppendLine()
            .AppendLine();

        if (!demoted.Any()) return;

        demoted.ForEach(
            (
                chatMember,
                index
            ) => {
                var number = index + 1;
                var nameLink = chatMember.User.GetNameLink();
                messageBuilder.Append(number).Append(". ").AppendLine(nameLink);
            }
        );

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: messageBuilder.ToTrimmedString(),
            parseMode: ParseMode.Html
        );
    }

    [JobDisplayName("Admin Checker {0}")]
    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
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

            var urlAddTo = await _botService.GetUrlStart("startgroup=new");
            var msgLeave = "Sepertinya saya bukan admin di grup ini, saya akan meninggalkan grup. Sampai jumpa!" +
                           "\n\nTerima kasih sudah menggunakan @MissZiziBot, silakan undang saya kembali jika diperlukan.";

            var inlineKeyboard = new InlineKeyboardMarkup
            (
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithUrl("👥 Dukungan", "https://t.me/WinTenDev"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithUrl("➕ Tambahkan ke Grup", urlAddTo)
                    }
                }
            );

            await _botClient.SendTextMessageAsync(
                chatId,
                msgLeave,
                ParseMode.Html,
                replyMarkup: inlineKeyboard
            );
            await _botClient.LeaveChatAsync(chatId);

            Log.Debug(
                "Checking Admin in ChatID '{ChatId}' job complete in {Elapsed}",
                chatId,
                sw.Elapsed
            );
            sw.Stop();

            await Task.Delay(5000);
        }
        catch (ApiRequestException requestException)
        {
            Log.Error(
                requestException,
                "Error when Check Admin on ChatID: '{ChatId}'",
                chatId
            );
            var setting = await _settingsService.GetSettingsByGroupCore(chatId);

            var exMessage = requestException.Message.ToLower();

            if (exMessage.IsContains("forbidden"))
            {
                var dateNow = DateTime.UtcNow;
                var diffDays = (dateNow - setting.UpdatedAt).TotalDays;
                Log.Debug(
                    "Last activity days in '{ChatId}' is {DiffDays:N2} days",
                    chatId,
                    diffDays
                );

                if (diffDays > AfterLeaveLimit)
                {
                    Log.Debug("This time is cleanup this chat!");
                    await _settingsService.DeleteSettings(chatId);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(
                e,
                "Error when Check Admin in ChatID: '{ChatId}'",
                chatId
            );
        }
    }
}