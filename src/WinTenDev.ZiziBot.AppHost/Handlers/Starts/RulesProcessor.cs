using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Starts;

public class RulesProcessor
{
    private readonly ILogger<RulesProcessor> _logger;
    private readonly RulesService _rulesService;

    public RulesProcessor(
        ILogger<RulesProcessor> logger,
        RulesService rulesService
    )
    {
        _logger = logger;
        _rulesService = rulesService;
    }

    public async Task<MessageResponseDto> Execute(string payload)
    {
        var response = new MessageResponseDto();
        var chatId = payload.ToInt64().FixChatId();
        var rules = await _rulesService.GetRulesAsync(chatId);
        var latestRule = rules.LastOrDefault();
        var ruleText = latestRule?.RuleText;

        if (latestRule == null) return response;

        response.MessageText = ruleText;
        response.DisableWebPreview = true;
        response.ReplyToMessageId = 0;

        return response;
    }
}