using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.Services.Telegram;

public class FeatureService
{
    private readonly ILogger<FeatureService> _logger;
    private readonly BotService _botService;
    private readonly FeatureConfig _featureConfig;

    public FeatureService(
        ILogger<FeatureService> logger,
        IOptionsSnapshot<FeatureConfig> featureConfig,
        BotService botService
    )
    {
        _logger = logger;
        _botService = botService;
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

    public async Task<ButtonParsed> GetFeatureConfig(string key)
    {
        var config = _featureConfig.Items?.FirstOrDefault(item => item.Key == key);

        var buttonMarkup = InlineKeyboardMarkup.Empty();

        var buttonParsed = new ButtonParsed
        {
            Markup = buttonMarkup
        };

        if (config == null)
        {
            _logger.LogWarning("ButtonConfig Not Found: {Key}", key);
            return buttonParsed;
        }

        var currentEnvironment = await _botService.CurrentEnvironment();

        var mergedCaption = config.Captions?
            .Where(caption => caption.MinimumLevel <= currentEnvironment)
            .Select(caption => caption.Sections.JoinStr("\n\n"))
            .JoinStr("\n\n");

        if (config?.Buttons != null)
        {
            buttonMarkup = new InlineKeyboardMarkup
            (
                config
                    .Buttons
                    .Select
                    (
                        x => x
                            .Select(y => InlineKeyboardButton.WithUrl(y.Text, y.Url.ToString()))
                    )
            );
        }

        buttonParsed.IsEnabled = config.IsEnabled;
        buttonParsed.AllowsAt = config.AllowsAt;
        buttonParsed.ExceptsAt = config.ExceptsAt;
        buttonParsed.Caption = mergedCaption;
        buttonParsed.Markup = buttonMarkup;

        return buttonParsed;
    }
}