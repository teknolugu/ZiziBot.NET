using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Serilog;
using SerilogTimings;
using SpamWatch.Types;

namespace WinTenDev.Zizi.Services.Internals;

/// <summary>
/// The anti spam service.
/// </summary>
public class AntiSpamService
{
    private readonly UsergeFedConfig _usergeFedConfig;
    private readonly CacheService _cacheService;
    private readonly ChatService _chatService;
    private readonly LocalizationService _localizationService;
    private readonly GlobalBanService _globalBanService;
    private readonly SettingsService _settingsService;
    private readonly WTelegramApiService _wTelegramApiService;
    private readonly SpamWatchConfig _spamWatchConfig;
    private ChatSetting _chatSetting;

    /// <summary>
    /// Initializes a new instance of the <see cref="AntiSpamService"/> class.
    /// </summary>
    /// <param name="spamWatchConfig"></param>
    /// <param name="usergeFedConfig"></param>
    /// <param name="cacheService"></param>
    /// <param name="chatService"></param>
    /// <param name="localizationService"></param>
    /// <param name="globalBanService">The global ban service.</param>
    /// <param name="settingsService"></param>
    /// <param name="wTelegramApiService"></param>
    public AntiSpamService(
        IOptionsSnapshot<SpamWatchConfig> spamWatchConfig,
        IOptionsSnapshot<UsergeFedConfig> usergeFedConfig,
        CacheService cacheService,
        ChatService chatService,
        LocalizationService localizationService,
        GlobalBanService globalBanService,
        SettingsService settingsService,
        WTelegramApiService wTelegramApiService
    )
    {
        _spamWatchConfig = spamWatchConfig.Value;
        _usergeFedConfig = usergeFedConfig.Value;
        _cacheService = cacheService;
        _chatService = chatService;
        _localizationService = localizationService;
        _globalBanService = globalBanService;
        _settingsService = settingsService;
        _wTelegramApiService = wTelegramApiService;
    }

    /// <summary>
    /// Checks the spam.
    /// </summary>
    /// <param name="chatId"></param>
    /// <param name="userId">The user id.</param>
    /// <param name="funcAntiSpamResult"></param>
    /// <returns>A Task.</returns>
    public async Task<AntiSpamResult> CheckSpam(
        long chatId,
        long userId,
        Func<AntiSpamResult, Task> funcAntiSpamResult = null
    )
    {
        var spamResult = new AntiSpamResult
        {
            UserId = userId
        };

        var isIgnored = _chatService.CheckUserIdIgnored(userId);

        if (isIgnored)
        {
            return spamResult;
        }

        _chatSetting = await _settingsService.GetSettingsByGroup(chatId);

        string nameLink;

        if (chatId != 0)
        {
            var chatMember = await _chatService.GetChatMemberAsync(chatId, userId);
            nameLink = chatMember.User.GetNameLink();
        }
        else
        {
            var fullUser = await _wTelegramApiService.GetFullUser(userId);
            nameLink = fullUser.users.Values.FirstOrDefault()?.GetNameLink();
        }

        var checkSpamWatchTask = CheckSpamWatch(userId);
        var checkCasBanTask = CheckCasBan(userId);
        var checkEs2BanTask = CheckEs2Ban(userId);
        var checkUsergeBanTask = CheckUsergeBan(userId);

        await Task.WhenAll(
            checkSpamWatchTask,
            checkCasBanTask,
            checkEs2BanTask,
            checkUsergeBanTask
        );

        var es2Ban = _chatSetting.EnableFedEs2 && checkEs2BanTask.Result;
        var swBan = _chatSetting.EnableFedSpamWatch && checkSpamWatchTask.Result;
        var casBan = _chatSetting.EnableFedCasBan && checkCasBanTask.Result;
        var usergeBan = checkUsergeBanTask.Result;
        var anyBan = swBan || casBan || es2Ban || usergeBan;

        if (!anyBan)
        {
            Log.Information("UserId {UserId} is passed on all Fed Ban", userId);
            return spamResult;
        }

        var banMessage = _localizationService.GetLoc(
            langCode: _chatSetting.LanguageCode,
            enumPath: GlobalBan.BanMessage,
            placeHolders: new List<(string placeholder, string value)>()
            {
                ("UserId", userId.ToString()),
                ("FullName", nameLink)
            }
        );

        var htmlMessage = HtmlMessage.Empty
            .TextBr(banMessage);

        if (es2Ban) htmlMessage.Url("https://t.me/WinTenDev", "- ES2 Global Ban").Br();
        if (casBan) htmlMessage.Url($"https://cas.chat/query?u={userId}", "- CAS Fed").Br();
        if (swBan) htmlMessage.Url("https://t.me/SpamWatchSupport", "- SpamWatch Fed");
        if (usergeBan) htmlMessage.Url("https://t.me/UsergeAntiSpamSupport", "- Userge Fed");

        spamResult = new AntiSpamResult()
        {
            MessageResult = htmlMessage.ToString(),
            IsAnyBanned = anyBan,
            IsEs2Banned = es2Ban,
            IsCasBanned = casBan,
            IsSpamWatched = swBan,
            IsUsergeBanned = usergeBan,
        };

        if (funcAntiSpamResult != null) await funcAntiSpamResult(spamResult);

        Log.Debug("AntiSpam Result {@Result}", spamResult);

        return spamResult;
    }

    /// <summary>
    /// Checks the es2 ban.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <returns>A Task.</returns>
    public async Task<bool> CheckEs2Ban(long userId)
    {
        var op = Operation.Begin("ES2 Fed Ban check for UserId: {UserId}", userId);

        var isBan = false;

        if (!_chatSetting.EnableFedEs2)
        {
            Log.Warning("ES2 Fed is disabled by Settings at ChatId: {ChatId}", _chatSetting.ChatId);
            op.Complete();

            return false;
        }

        try
        {
            var ban = await _globalBanService.GetGlobalBanById(userId);

            isBan = ban != null;
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "Error check ES2 Ban for UserId {UserId}",
                userId
            );
        }

        Log.Debug(
            "ES2 Ban result for UserId: '{UserId}' ? '{IsBan}'",
            userId,
            isBan
        );

        op.Complete();

        return isBan;
    }

    /// <summary>
    /// Checks the spam watch.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <returns>A Task.</returns>
    public async Task<bool> CheckSpamWatch(long userId)
    {
        var op = Operation.Begin("SpamWatch Fed check for UserId: {UserId}", userId);
        var cacheKey = $"ban_spamwatch_{userId}";

        var isEnabled = _spamWatchConfig.IsEnabled;
        var baseUrl = _spamWatchConfig.BaseUrl;
        var apiToken = _spamWatchConfig.ApiToken;

        if (!isEnabled)
        {
            Log.Warning("SpamWatch is disabled by Administrator");
            op.Complete();

            return false;
        }

        var validate = await _spamWatchConfig.ValidateAsync<SpamWatchConfigValidator, SpamWatchConfig>();
        if (!validate.IsValid)
        {
            Log.Warning("SpamWatch is disabled because not properly configured");
            op.Complete();

            return false;
        }

        if (!_chatSetting.EnableFedSpamWatch)
        {
            Log.Warning("SpamWatch is disabled by Settings at ChatId: {ChatId}", _chatSetting.ChatId);
            op.Complete();

            return false;
        }

        var check = await _cacheService.GetOrSetAsync(
            cacheKey: cacheKey,
            action: async () => {
                var isBan = false;

                try
                {
                    var check = await baseUrl
                        .AppendPathSegment("banlist")
                        .AppendPathSegment(userId)
                        .WithOAuthBearerToken(apiToken)
                        .AllowHttpStatus("404")
                        .GetJsonAsync<Ban>();

                    isBan = check.Reason.IsNotNullOrEmpty();
                    Log.Debug("SpamWatch Result: {@V}", check);
                }
                catch (FlurlHttpException ex)
                {
                    if (!ex.Message.Contains("timeout", StringComparison.CurrentCultureIgnoreCase))
                    {
                        var callHttpStatus = ex.Call.HttpResponseMessage?.StatusCode;

                        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                        switch (callHttpStatus)
                        {
                            case HttpStatusCode.NotFound:
                                Log.Debug("No UserId {UserId} found at SpamWatch Fed", userId);
                                isBan = false;
                                break;

                            case HttpStatusCode.Unauthorized:
                                Log.Warning("Please check your SpamWatch API Token!");
                                Log.Error(ex, "SpamWatch Exception");
                                break;

                            default:
                                Log.Error(ex, "SpamWatch - Unknown call status");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(
                        ex,
                        "SpamWatch Exception UserId: {UserId}",
                        userId
                    );
                }

                return isBan;
            }
        );

        Log.Debug(
            "SpamWatch result for UserId: '{UserId}' ? '{IsBan}'",
            userId,
            check
        );
        op.Complete();

        return check;
    }

    /// <summary>
    /// Are the cas ban async.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <returns>A Task.</returns>
    public async Task<bool> CheckCasBan(long userId)
    {
        var op = Operation.Begin("CAS Fed check for UserId: {UserId}", userId);
        var casCacheKey = $"ban_cas_{userId}";

        if (!_chatSetting.EnableFedCasBan)
        {
            Log.Warning("CAS Fed is disabled by Settings at ChatId: {ChatId}", _chatSetting.ChatId);
            op.Complete();

            return false;
        }

        try
        {
            var data = await _cacheService.GetOrSetAsync(
                cacheKey: casCacheKey,
                action: async () => {
                    var url = "https://api.cas.chat/check".SetQueryParam("user_id", userId);
                    var resp = await url.GetJsonAsync<CasBan>();

                    return resp;
                }
            );

            Log.Debug("CasBan Result: {@V}", data);

            var isBan = data.Ok;
            Log.Debug(
                "CAS Ban result for UserId: '{UserId}' ? '{IsBan}'",
                userId,
                isBan
            );

            op.Complete();

            return isBan;
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "CAS Ban Exception UserId: {UserId}",
                userId
            );

            op.Complete();
            return false;
        }
    }

    public async Task<bool> CheckUsergeBan(long userId)
    {
        if (!_usergeFedConfig.IsEnabled) return false;

        var op = Operation.Begin("Userge Fed check for UserId: {UserId}", userId);

        try
        {
            var usergeGBanResult = await _cacheService.GetOrSetAsync(
                cacheKey: "ban_userge_" + userId,
                action: async () => {
                    var usergeGBanResult = await _usergeFedConfig.BaseUrl
                        .OpenFlurlSession()
                        .AppendPathSegment("ban/")
                        .SetQueryParam("user_id", userId)
                        .SetQueryParam("api_key", _usergeFedConfig.ApiToken)
                        .GetJsonAsync<UsergeGBanResult>();

                    return usergeGBanResult;
                }
            );

            op.Complete();
            return usergeGBanResult.Success;
        }
        catch (Exception e)
        {
            op.Complete();
            return false;
        }
    }
}