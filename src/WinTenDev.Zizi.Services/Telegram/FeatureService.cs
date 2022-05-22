using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoreLinq;
using NMemory.Tables;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.NMemory;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.Services.Telegram;

public class FeatureService
{
    private readonly ILogger<FeatureService> _logger;
    private readonly BotService _botService;
    private readonly RateLimitingInMemory _rateLimitingInMemory;
    private readonly FeatureConfig _featureConfig;

    public ITable<FeatureCooldown> FeatureCooldowns => _rateLimitingInMemory.FeatureCooldowns;

    public FeatureService(
        ILogger<FeatureService> logger,
        IOptionsSnapshot<FeatureConfig> featureConfig,
        BotService botService,
        RateLimitingInMemory rateLimitingInMemory
    )
    {
        _logger = logger;
        _botService = botService;
        _rateLimitingInMemory = rateLimitingInMemory;
        _featureConfig = featureConfig.Value;
    }

    public bool IsEnabled()
    {
        return _featureConfig.IsEnabled;
    }

    public FeatureConfig GetFeatureConfigs()
    {
        return _featureConfig;
    }

    public async Task<ButtonParsed> GetFeatureConfig(string featureName)
    {
        var config = _featureConfig.Items?.FirstOrDefault(item => item.Key == featureName);

        var buttonMarkup = InlineKeyboardMarkup.Empty();

        var buttonParsed = new ButtonParsed
        {
            FeatureName = featureName,
            Markup = buttonMarkup
        };

        if (config == null)
        {
            _logger.LogWarning("ButtonConfig Not Found: {Key}", featureName);
            return buttonParsed;
        }

        var currentEnvironment = await _botService.CurrentEnvironment();

        var mergedCaption = config.Captions?
            .Where(caption => caption.MinimumLevel <= currentEnvironment)
            .Select(caption => caption.Sections.JoinStr(" "))
            .JoinStr("\n\n");

        List<IEnumerable<InlineKeyboardButton>> button = null;

        if (config.Buttons != null)
        {
            button = config
                .Buttons
                .Select
                (
                    x => x
                        .Select(y => InlineKeyboardButton.WithUrl(y.Text, y.Url.ToString()))
                )
                .ToList();

            buttonMarkup = new InlineKeyboardMarkup(button);
        }

        buttonParsed.IsEnabled = config.IsEnabled;
        buttonParsed.AllowsAt = config.AllowsAt;
        buttonParsed.ExceptsAt = config.ExceptsAt;

        buttonParsed.Caption = mergedCaption;
        buttonParsed.Markup = buttonMarkup;
        buttonParsed.KeyboardButton = button;

        buttonParsed.IsApplyRateLimit = config.RateLimitSpan != null;
        if (buttonParsed.IsApplyRateLimit)
        {
            buttonParsed.RateLimitTimeSpan = config.RateLimitSpan.ToTimeSpan();
            buttonParsed.NextAvailable = buttonParsed.RateLimitTimeSpan.ToDateTime(useUtc: true);
        }

        return buttonParsed;
    }

    public bool CheckCooldown(FeatureCooldown featureCooldown)
    {
        var allCooldowns = FeatureCooldowns.AsQueryable().ToList();
        var currentChatCooldowns = allCooldowns.Where(
                x =>
                    x.ChatId == featureCooldown.ChatId &&
                    x.FeatureName == featureCooldown.FeatureName &&
                    x.NextAvailable >= DateTime.UtcNow
            )
            .ToList();

        var isNeedCooldown = currentChatCooldowns.Count > 0;

        _logger.LogInformation(
            "Command: {FeatureName} is need cooldown at ChatId: {ChatId}. Next Available: {NextAvailable}",
            featureCooldown.FeatureName,
            featureCooldown.ChatId,
            featureCooldown.NextAvailable
        );

        FeatureCooldowns.Where(
            x =>
                x.ChatId == featureCooldown.ChatId &&
                x.FeatureName == featureCooldown.FeatureName &&
                x.NextAvailable <= DateTime.UtcNow
        ).ForEach(
            entity =>
                FeatureCooldowns.Delete(entity)
        );

        _logger.LogDebug(
            "RateLimiting at {ChatId}. Current Cooldown: {@CurrentCooldown}",
            featureCooldown.ChatId,
            currentChatCooldowns
        );

        featureCooldown.Guid = Guid.NewGuid().ToString();

        FeatureCooldowns.Insert(featureCooldown);

        return isNeedCooldown;
    }
}
