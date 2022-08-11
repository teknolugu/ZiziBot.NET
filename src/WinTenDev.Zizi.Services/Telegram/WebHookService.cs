using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace WinTenDev.Zizi.Services.Telegram;

public class WebHookService
{
    private readonly ILogger<WebHookService> _logger;
    private readonly ITelegramBotClient _botClient;
    private readonly WebHookChatService _webHookChatService;
    private WebHookChat _webHookChat;

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
        var stopwatch = Stopwatch.StartNew();
        var requestStartedOn = (DateTime)request.HttpContext.Items["RequestStartedOn"]!;
        var responseTime = DateTime.UtcNow - requestStartedOn;

        var webHookSource = request.GetWebHookSource();

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
        _webHookChat = await _webHookChatService.GetWebHookById(hookId?.ToString());

        if (_webHookChat == null)
        {
            result.ParsedMessage = "WebHook not found for id: " + hookId;

            return result;
        }

        var isDebugMode = await CheckDebugMode(request);
        _logger.LogInformation("WebHook Chat destination for HookId: {HookId} => {@WebHookChat}",
            hookId,
            _webHookChat
        );

        var chatId = _webHookChat.ChatId;
        var source = result.WebhookSource;
        var message = result.ParsedMessage;

        if (isDebugMode)
        {
            message += "\n\n#DEBUG_MODE";
        }

        var sentMessage = await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: message,
            parseMode: ParseMode.Html,
            disableWebPagePreview: true
        );

        _logger.LogInformation("Sent WebHook from {Source} to ChatId: {ChatId} Message: {@Message}",
            source,
            chatId,
            sentMessage
        );

        result.ResponseTime = responseTime.ToString();
        result.ExecutionTime = stopwatch.Elapsed.ToString();

        return result;
    }

    private async Task<bool> CheckDebugMode(HttpRequest request)
    {
        var isDebug = request.Query
            .FirstOrDefault(pair => pair.Key == "debug").Value
            .FirstOrDefault().ToBool();

        if (!isDebug) return false;

        var requestStartedOn = (DateTime)request.HttpContext.Items["RequestStartedOn"]!;
        var webHookSource = request.GetWebHookSource();
        var bodyString = await request.GetRawBodyAsync();
        var headerJson = request.Headers.ToJson(indented: true);

        // var dateStamp = requestStartedOn.ToString("yyyy-MM-dd/HH-mm-ss");
        // var requestPath = request.Path.Value;
        // await bodyString.WriteTextAsync($"{requestPath}/{webHookSource}/{dateStamp}.json", Formatting.Indented);

        await _botClient.SendMediaGroupAsync(
            chatId: _webHookChat.ChatId,
            media: new List<IAlbumInputMedia>()
            {
                new InputMediaDocument(bodyString.ToInputMedia("payload.json"))
                {
                    Caption = "Payload"
                },
                new InputMediaDocument(headerJson.ToInputMedia("headers.json"))
                {
                    Caption = "Headers"
                }
            });
        return true;
    }
}