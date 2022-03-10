using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Text;
using WinTenDev.ZiziBot.AppHost.Handlers.Callbacks;

namespace WinTenDev.ZiziBot.AppHost.Handlers;

public class CallbackQueryHandler : IUpdateHandler
{
    private readonly TelegramService _telegramService;
    private readonly ActionCallback _actionCallback;
    private readonly HelpCallback _helpCallback;
    private readonly PingCallback _pingCallback;
    private readonly RssCtlCallback _rssCtlCallback;
    private readonly SettingsCallback _settingsCallback;
    private readonly VerifyCallback _verifyCallback;

    public CallbackQueryHandler(
        TelegramService telegramService,
        ActionCallback actionCallback,
        HelpCallback helpCallback,
        PingCallback pingCallback,
        RssCtlCallback rssCtlCallback,
        SettingsCallback settingsCallback,
        VerifyCallback verifyCallback
    )
    {
        _telegramService = telegramService;
        _actionCallback = actionCallback;
        _helpCallback = helpCallback;
        _pingCallback = pingCallback;
        _rssCtlCallback = rssCtlCallback;
        _settingsCallback = settingsCallback;
        _verifyCallback = verifyCallback;
    }

    public async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        CancellationToken cancellationToken
    )
    {
        await _telegramService.AddUpdateContext(context);

        var callbackQuery = context.Update.CallbackQuery;
        _telegramService.CallBackMessageId = callbackQuery.Message.MessageId;

        Log.Verbose("CallbackQuery: {Json}", callbackQuery.ToJson(true));

        var partsCallback = callbackQuery.Data.SplitText(" ");
        Log.Debug("Callbacks: {CB}", partsCallback);

        switch (partsCallback.ElementAtOrDefault(0))// Level 0
        {
            case "pong":
            case "PONG":
                var pingResult = await _pingCallback.ExecuteAsync();
                Log.Information("PingResult: {0}", pingResult.ToJson(true));
                break;

            case "action":
                var actionResult = await _actionCallback.ExecuteAsync();
                Log.Information("ActionResult: {V}", actionResult.ToJson(true));
                break;

            case "help":
                var helpResult = await _helpCallback.ExecuteAsync();
                Log.Information("HelpResult: {V}", helpResult.ToJson(true));
                break;

            case "rssctl":
                var result = await _rssCtlCallback.ExecuteAsync();
                break;

            case "setting":
                var settingResult = await _settingsCallback.ExecuteToggleAsync();
                Log.Information("SettingsResult: {V}", settingResult.ToJson(true));
                break;

            case "verify":
                var verifyResult = await _verifyCallback.ExecuteVerifyAsync();
                Log.Information("VerifyResult: {V}", verifyResult.ToJson(true));
                break;
        }

        await next(context, cancellationToken);
    }
}