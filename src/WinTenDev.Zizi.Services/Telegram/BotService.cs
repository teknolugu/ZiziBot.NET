using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace WinTenDev.Zizi.Services.Telegram;

public class BotService
{
    private readonly ILogger<BotService> _logger;
    private readonly EventLogConfig _eventLogConfig;
    private readonly CommandConfig _commandConfig;
    private readonly ButtonConfig _buttonConfig;
    private readonly ITelegramBotClient _botClient;
    private readonly CacheService _cacheService;

    public BotService(
        ILogger<BotService> logger,
        IOptionsSnapshot<ButtonConfig> buttonConfig,
        IOptionsSnapshot<CommandConfig> commandConfig,
        IOptionsSnapshot<EventLogConfig> eventLogConfig,
        ITelegramBotClient botClient,
        CacheService cacheService
    )
    {
        _logger = logger;
        _eventLogConfig = eventLogConfig.Value;
        _commandConfig = commandConfig.Value;
        _buttonConfig = buttonConfig.Value;
        _botClient = botClient;
        _cacheService = cacheService;
    }

    public async Task GetWebHookInfo()
    {
        var webhookInfo = await _botClient.GetWebhookInfoAsync();
        _logger.LogInformation($"Webhook info: {webhookInfo.Url}");
        var webHookInfoStr = webhookInfo.ParseWebHookInfo();

        await _botClient.SendTextMessageAsync("", webHookInfoStr.ToString());
    }

    public async Task<User> GetMeAsync()
    {
        var getMe = await _cacheService.GetOrSetAsync(
            cacheKey: "get-me",
            action: async () => {
                var getMe = await _botClient.GetMeAsync();
                return getMe;
            }
        );

        return getMe;
    }

    public async Task<string> GetUrlStart(string param)
    {
        var getMe = await GetMeAsync();
        var username = getMe.Username;
        var urlStart = $"https://t.me/{username}?{param}";

        return urlStart;
    }

    public async Task<bool> IsDev()
    {
        var me = await GetMeAsync();
        var isBeta = me.Username?.Contains("dev", StringComparison.OrdinalIgnoreCase) ?? false;
        _logger.LogDebug(
            "Is Bot {Me} IsDev: {IsBeta}",
            me,
            isBeta
        );

        return isBeta;
    }

    public async Task<bool> IsBeta()
    {
        var me = await GetMeAsync();
        var isBeta = me.Username?.Contains("beta", StringComparison.OrdinalIgnoreCase) ?? false;
        _logger.LogDebug(
            "Is Bot {Me} IsBeta: {IsBeta}",
            me,
            isBeta
        );

        return isBeta;
    }

    public async Task<bool> IsProd()
    {
        var me = await GetMeAsync();
        var isBeta = !me.Username?.ContainsListStr("beta", "dev") ?? false;
        _logger.LogDebug(
            "Is Bot {Me} IsProd: {IsBeta}",
            me,
            isBeta
        );

        return isBeta;
    }

    public async Task<BotEnvironmentLevel> CurrentEnvironment()
    {
        var environment = BotEnvironmentLevel.Development;

        if (await IsDev()) environment = BotEnvironmentLevel.Development;
        if (await IsBeta()) environment = BotEnvironmentLevel.Staging;
        if (await IsProd()) environment = BotEnvironmentLevel.Production;

        var me = await GetMeAsync();
        _logger.LogInformation(
            "Bot {Me} is at Environment: {Environment}",
            me,
            environment
        );

        return environment;
    }

    public async Task<IEnumerable<BotCommand>> GetCommandConfigs()
    {
        var getMe = await GetMeAsync();
        var forResolve = new List<(string placeholder, string value)>()
        {
            ("BotName", getMe.GetFullName())
        };

        var commandConfigs = _commandConfig
            .CommandItems?
            .Select(
                item => new BotCommand()
                {
                    Command = item.Command,
                    Description = item.Description.ResolveVariable(forResolve),
                }
            );

        return commandConfigs;
    }

    public async Task EnsureCommandRegistration()
    {
        var botCommands = await GetCommandConfigs();

        if (botCommands != null &&
            _commandConfig.EnsureOnStartup)
        {
            await _botClient.SetMyCommandsAsync(botCommands);
        }
        else
        {
            await _botClient.DeleteMyCommandsAsync();
        }
    }

    public List<ButtonItem> GetButtonConfigAll()
    {
        var buttonItems = _buttonConfig.Items;

        return buttonItems;
    }

    public async Task<ButtonParsed> GetButtonConfig(string key)
    {
        var items = GetButtonConfigAll();
        var item = items.FirstOrDefault(buttonItem => buttonItem.Key == key);

        var buttonMarkup = InlineKeyboardMarkup.Empty();

        var buttonParsed = new ButtonParsed
        {
            Markup = buttonMarkup
        };

        if (item == null)
        {
            _logger.LogWarning("ButtonConfig Not Found: {Key}", key);
            return buttonParsed;
        }

        var currentEnvironment = await CurrentEnvironment();

        var mergedCaption = item.Data?.Captions?
            .Where(caption => caption.MinimumLevel <= currentEnvironment)
            .Select(caption => caption.Sections.JoinStr("\n\n"))
            .JoinStr("\n\n");

        if (item?.Data?.Buttons != null)
        {
            buttonMarkup = new InlineKeyboardMarkup(
                item
                    .Data
                    .Buttons
                    .Select(
                        x => x
                            .Select(y => InlineKeyboardButton.WithUrl(y.Text, y.Url))
                    )
            );
        }

        buttonParsed.Caption = mergedCaption;
        buttonParsed.Markup = buttonMarkup;

        return buttonParsed;
    }

    public async Task SendStartupNotification()
    {
        var channelId = _eventLogConfig.ChannelId;
        var appHostInfo = AppHostUtil.GetAppHostInfo(includePath: true);

        var htmlMessage = HtmlMessage.Empty
            .Bold("Bot has started successfully").Br().Br()
            .Text(appHostInfo);

        await _botClient.SendTextMessageAsync(
            chatId: channelId,
            text: htmlMessage.ToString(),
            parseMode: ParseMode.Html
        );
    }
}