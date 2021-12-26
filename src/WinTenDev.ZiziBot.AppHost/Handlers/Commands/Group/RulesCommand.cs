using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Group;

public class RulesCommand : CommandBase
{
    private readonly SettingsService _settingsService;
    private readonly TelegramService _telegramService;
    private readonly PrivilegeService _privilegeService;

    public RulesCommand(
        SettingsService settingsService,
        TelegramService telegramService,
        PrivilegeService privilegeService
    )
    {
        _settingsService = settingsService;
        _telegramService = telegramService;
        _privilegeService = privilegeService;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);
        var fromId = _telegramService.FromId;
        var msg = context.Update.Message;

        var chatId = _telegramService.ChatId;

        var sendText = "Under maintenance";
        if (_telegramService.IsPrivateChat)
        {
            Log.Debug("Rules only for Group");
            return;
        }

        if (_privilegeService.IsFromSudo(fromId))
        {
            var settings = await _settingsService.GetSettingsByGroup(chatId);

            Log.Information("Settings: {0}", settings.ToJson(true));
            // var rules = settings.Rows[0]["rules_text"].ToString();
            var rules = settings.RulesText;
            Log.Debug("Rules: \n{0}", rules);
            sendText = rules;
        }

        await _telegramService.SendTextMessageAsync(sendText);
    }
}