using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace WinTenDev.Zizi.Services.Telegram;

public class WebHookService
{
    private readonly ILogger<WebHookService> _logger;
    private readonly ITelegramBotClient _botClient;
    private readonly WebHookChatService _webHookChatService;

    public WebHookService(
        ILogger<WebHookService> logger,
        ITelegramBotClient botClient,
        WebHookChatService webHookChatService
    )
    {
        _logger = logger;
        _botClient = botClient;
        _webHookChatService = webHookChatService;
    }

    public async Task<WebHookResult> ProcessingRequest(HttpRequest request)
    {
        var webHookSource = request.GetWebHookSource();
        var bodyString = await request.GetRawBodyAsync();

        var requestPath = request.Path.Value;
        var dateStamp = DateTime.Now.ToString("yyyy-MM-dd/HH-mm-ss");
        await bodyString.WriteTextAsync($"{requestPath}/{webHookSource}/{dateStamp}.json", Formatting.Indented);

        var result = webHookSource switch
        {
            WebhookSource.GitHub => await this.ProcessGithubWebHook(request),
            _ => new WebHookResult()
            {
                WebhookSource = WebhookSource.Unknown,
                ParsedMessage = "Unknown Webhook Source"
            }
        };

        var hookId = request.RouteValues.ElementAtOrDefault(2).Value;
        var webHookChat = await _webHookChatService.GetWebHookById(hookId.ToString());

        var chatId = webHookChat.ChatId;
        var source = result.WebhookSource;
        var message = result.ParsedMessage;

        var sentMessage = await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: message,
            parseMode: ParseMode.Html,
            disableWebPagePreview: true
        );

        _logger.LogInformation(
            "Sent WebHook from {Source} to ChatId: {ChatId} Message: {@Message}",
            source,
            chatId,
            sentMessage
        );

        return result;
    }

    private async Task SendMessage(WebHookResult result)
    {
        var chatId = "-1001404591750";
        var source = result.WebhookSource;
        var message = result.WebhookSource.ToString();

        var sentMessage = await _botClient.SendTextMessageAsync(chatId, message);

        _logger.LogInformation(
            "Sent WebHook from {Source} to ChatId: {ChatId} Message: {@Message}",
            source,
            chatId,
            sentMessage
        );
    }
}