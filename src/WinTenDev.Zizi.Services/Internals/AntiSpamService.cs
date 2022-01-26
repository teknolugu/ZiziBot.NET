using System;
using System.Net;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Serilog;
using SerilogTimings;
using SpamWatch.Types;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Models.Validators;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.Services.Internals;

/// <summary>
/// The anti spam service.
/// </summary>
public class AntiSpamService
{
    private readonly CacheService _cacheService;
    private readonly ChatService _chatService;
    private readonly GlobalBanService _globalBanService;
    private readonly SpamWatchConfig _spamWatchConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="AntiSpamService"/> class.
    /// </summary>
    /// <param name="spamWatchConfig"></param>
    /// <param name="cacheService"></param>
    /// <param name="chatService"></param>
    /// <param name="globalBanService">The global ban service.</param>
    public AntiSpamService(
        IOptionsSnapshot<SpamWatchConfig> spamWatchConfig,
        CacheService cacheService,
        ChatService chatService,
        GlobalBanService globalBanService
    )
    {
        _spamWatchConfig = spamWatchConfig.Value;
        _cacheService = cacheService;
        _chatService = chatService;
        _globalBanService = globalBanService;
    }

    /// <summary>
    /// Checks the spam.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="funcAntiSpamResult"></param>
    /// <returns>A Task.</returns>
    public async Task<AntiSpamResult> CheckSpam(
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

        var spamWatchTask = CheckSpamWatch(userId);
        var casBanTask = CheckCasBan(userId);
        var gBanTask = CheckEs2Ban(userId);

        await Task.WhenAll(spamWatchTask, casBanTask, gBanTask);

        var swBan = spamWatchTask.Result;
        var casBan = casBanTask.Result;
        var es2Ban = gBanTask.Result;
        var anyBan = swBan || casBan || es2Ban;

        if (!anyBan)
        {
            Log.Information("UserId {UserId} is passed on all Fed Ban", userId);
        }
        else
        {
            var banMsg = $"Pengguna {userId} telah di Ban di Federasi";

            if (es2Ban) banMsg += "\n- ES2 Global Ban";
            if (casBan) banMsg += "\n- CAS Fed";
            if (swBan) banMsg += "\n- SpamWatch Fed";

            spamResult = new AntiSpamResult()
            {
                MessageResult = banMsg,
                IsAnyBanned = anyBan,
                IsEs2Banned = es2Ban,
                IsCasBanned = casBan,
                IsSpamWatched = swBan
            };
        }

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
        var op = Operation.Begin("AntiSpam - ES2 Ban check for UserId: {UserId}", userId);

        var isBan = false;
        var cacheKey = $"ban-es2_{userId}";

        try
        {
            var banResult = await _cacheService.GetOrSetAsync(cacheKey, async () => {
                var ban = await _globalBanService.GetGlobalBanByIdCore(userId);

                var banResult = new GlobalBanResult()
                {
                    IsBanned = ban != null,
                    Data = ban
                };

                return banResult;
            });

            isBan = banResult.IsBanned;
        }
        catch (Exception exception)
        {
            Log.Error(exception, "AntiSpam - Error check ES2 Ban for UserId {UserId}", userId);
        }

        Log.Debug("ES2 Ban result for UserId: '{UserId}' ? '{IsBan}'", userId, isBan);

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
        var op = Operation.Begin("AntiSpam - SpamWatch check for UserId: {UserId}", userId);
        var cacheKey = $"ban-sw_{userId}";

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

        var check = await _cacheService.GetOrSetAsync(cacheKey, async () => {
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
                Log.Error(ex, "SpamWatch Exception");
            }

            return isBan;
        });

        Log.Debug("SpamWatch result for UserId: '{UserId}' ? '{IsBan}'", userId, check);
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
        var op = Operation.Begin("AntiSpam - CAS check for UserId: {UserId}", userId);
        var casCacheKey = $"ban-cas_{userId}";
        try
        {
            var data = await _cacheService.GetOrSetAsync(casCacheKey, async () => {
                var url = "https://api.cas.chat/check".SetQueryParam("user_id", userId);
                var resp = await url.GetJsonAsync<CasBan>();

                return resp;
            });

            Log.Debug("CasBan Result: {@V}", data);

            var isBan = data.Ok;
            Log.Debug("CAS Ban result for UserId: '{UserId}' ? '{IsBan}'", userId, isBan);

            op.Complete();

            return isBan;
        }
        catch
        {
            op.Complete();
            return false;
        }
    }
}