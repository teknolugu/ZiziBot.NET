using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace WinTenDev.Zizi.Services.Starts;

public class RulesProcessor
{
    private readonly ILogger<RulesProcessor> _logger;
    private readonly ChatService _chatService;
    private readonly RulesService _rulesService;

    public RulesProcessor(
        ILogger<RulesProcessor> logger,
        ChatService chatService,
        RulesService rulesService
    )
    {
        _logger = logger;
        _chatService = chatService;
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

        var chat = await _chatService.GetChatAsync(chatId);
        var chatNameLink = chat.GetChatNameLink();
        var lastUpdate = latestRule.UpdatedAt;

        var ruleTextWithTitle = $"📜 Rules di <b>{chatNameLink}</b>\n" +
                                $"\n{ruleText}" +
                                $"\n\n<b>Diperbarui: </b> {lastUpdate.ToDetailDateTimeString()}";

        response.MessageText = ruleTextWithTitle;
        response.DisableWebPreview = true;
        response.ReplyToMessageId = 0;

        return response;
    }
}