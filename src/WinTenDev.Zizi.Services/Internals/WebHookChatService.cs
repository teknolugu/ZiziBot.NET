using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Services.Internals;

public class WebHookChatService
{
    private readonly ILogger<WebHookChatService> _logger;
    private readonly WebApiConfig _webApiConfig;

    public WebHookChatService(
        ILogger<WebHookChatService> logger,
        IOptionsSnapshot<WebApiConfig> webApiConfig
    )
    {
        _logger = logger;
        _webApiConfig = webApiConfig.Value;
    }

    public async Task<WebHookChat> GetWebHookChat(long chatId)
    {
        var findHook = await DB.Find<WebHookChat>()
            .Match(chat => chat.ChatId == chatId)
            .ExecuteFirstAsync();

        return findHook;
    }

    public async Task<WebHookChat> GetWebHookById(string hookId)
    {
        var findHook = await DB.Find<WebHookChat>()
            .Match(chat => chat.ID == hookId)
            .ExecuteFirstAsync();

        return findHook;
    }

    public async Task<WebHookChat> GenerateWebHookChat(long chatId)
    {
        var findHook = await GetWebHookChat(chatId);

        if (findHook != null)
            return findHook;

        var webHookChat = new WebHookChat()
        {
            ChatId = chatId
        };

        await webHookChat.SaveAsync();

        return webHookChat;
    }

    public async Task<string> GetWebHookUrl(long chatId)
    {
        var webHookChat = await GenerateWebHookChat(chatId);
        var webHookUrl = $"{_webApiConfig.BaseUrl}/webhook/{webHookChat.ID}";

        _logger.LogInformation($"WebHookUrl: {webHookUrl}");

        return webHookUrl;
    }
}