using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyCaching.Core;
using Microsoft.Extensions.Logging;
using NMemory.Tables;
using SerilogTimings;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Services.NMemory;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.Services.Internals;

/// <summary>
/// Track user property changes
/// </summary>
public class MataService
{
    private readonly string baseKey = "zizi-mata";
    private readonly ILogger<MataService> _logger;
    private readonly IEasyCachingProvider _cachingProvider;
    private readonly HitActivityInMemory _hitActivityInMemory;
    private readonly SettingsService _settingsService;
    private readonly ITable<HitActivity> _mataActivities;

    /// <summary>
    /// Mata constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="cachingProvider"></param>
    /// <param name="hitActivityInMemory"></param>
    /// <param name="settingsService"></param>
    public MataService
    (
        ILogger<MataService> logger,
        IEasyCachingProvider cachingProvider,
        HitActivityInMemory hitActivityInMemory,
        SettingsService settingsService
    )
    {
        _logger = logger;
        _cachingProvider = cachingProvider;
        _hitActivityInMemory = hitActivityInMemory;
        _settingsService = settingsService;

        _mataActivities = hitActivityInMemory.MataActivities;
    }

    private string GetCacheKey(long fromId)
    {
        return $"{baseKey}_{fromId}";
    }

    public async Task<string> CheckMata(HitActivity hitActivity)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var chatId = hitActivity.ChatId;
            var fromId = hitActivity.FromId;
            var fromUsername = hitActivity.FromUsername;
            var fromFName = hitActivity.FromFirstName;
            var fromLName = hitActivity.FromLastName;

            var chatSettings = await _settingsService.GetSettingsByGroup(chatId);
            if (!chatSettings.EnableZiziMata)
            {
                _logger.LogInformation("MataZizi is disabled in this Group!. Completed in {Elapsed}", sw.Elapsed);
                sw.Stop();
                return string.Empty;
            }

            _logger.LogInformation("Starting SangMata check..");

            var hitActivityCache = GetLastMata(fromId);
            if (hitActivityCache == null)
            {
                _logger.LogInformation("This may first Hit from User {V}. In {V1}", fromId, sw.Elapsed);

                SaveMata(fromId, hitActivity);

                return string.Empty;
            }

            var changesCount = 0;
            var msgBuild = new StringBuilder();

            msgBuild.AppendLine("😽 <b>MataZizi</b>");
            msgBuild.Append("<b>UserID:</b> ").Append(fromId).AppendLine();

            if (fromUsername != hitActivity.FromUsername)
            {
                _logger.LogDebug("Username changed detected!");
                msgBuild.Append("Mengubah Username menjadi @").AppendLine(fromUsername);
                changesCount++;
            }

            if (fromFName != hitActivity.FromFirstName)
            {
                _logger.LogDebug("First Name changed detected!");
                msgBuild.Append("Mengubah nama depan menjadi ").AppendLine(fromFName);
                changesCount++;
            }

            if (fromLName != hitActivity.FromLastName)
            {
                _logger.LogDebug("Last Name changed detected!");
                msgBuild.Append("Mengubah nama belakang menjadi ").AppendLine(fromLName);
                changesCount++;
            }

            if (changesCount <= 0) return string.Empty;

            var mataResult = msgBuild.ToString().Trim();

            SaveMata(fromId, hitActivity);

            _logger.LogDebug("Complete update Cache");

            return mataResult;

            _logger.LogInformation("MataZizi completed in {Elapsed}. Changes: {ChangesCount}", sw.Elapsed, changesCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error SangMata");
        }

        return string.Empty;

        sw.Stop();
    }

    public HitActivity GetLastMata(long fromId)
    {
        var activities = _mataActivities.AsEnumerable();

        return activities.FirstOrDefault();
    }

    public void SaveMata(
        long fromId,
        HitActivity hitActivity
    )
    {
        var op = Operation.Begin("Saving Mata for UserId: {UserId} at ChatId: {ChatId}",
            hitActivity.FromId, hitActivity.ChatId);
        // _sangMata.Where(activity => activity.FromId == fromId).Delete();

        hitActivity.Guid = Guid.NewGuid().ToString();

        _hitActivityInMemory.MataActivities.Insert(hitActivity);

        op.Complete();
    }

    public async Task<CacheValue<HitActivity>> GetMataCore(long fromId)
    {
        var key = GetCacheKey(fromId);
        var hitActivity = await _cachingProvider.GetAsync<HitActivity>(key);
        return hitActivity;
    }

    public async Task SaveMataAsync(
        long fromId,
        HitActivity hitActivity
    )
    {
        var key = GetCacheKey(fromId);
        var timeSpan = TimeUtil.YearSpan(30);
        _logger.LogDebug("Saving Mata into cache with Key: '{Key}'. Span: {TimeSpan}", key, timeSpan);
        await _cachingProvider.SetAsync(key, hitActivity, timeSpan);
    }
}