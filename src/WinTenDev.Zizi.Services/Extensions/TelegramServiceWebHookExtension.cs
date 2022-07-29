using System;
using System.Threading.Tasks;

namespace WinTenDev.Zizi.Services.Extensions;

public static class TelegramServiceWebHookExtension
{
    public static async Task GenerateWebHook(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;
        if (!await telegramService.CheckUserPermission())
        {
            await telegramService.SendTextMessageAsync(
                sendText: "Anda tidak dapat melakukannya Obrolan ini. Anda dapat mengaturnya di Japri",
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
                includeSenderMessage: true
            );
        }

        var webHookChatService = telegramService.GetRequiredService<WebHookChatService>();

        var webHookChat = await webHookChatService.GetWebHookUrl(chatId);

        var htmlMessage = HtmlMessage.Empty
            .Bold("🪝 Zizi WebHook (Alpha)").Br()
            .Text("Berikut adalah URL untuk WebHook").Br()
            .Bold("URL: ").CodeBr(webHookChat)
            .Br()
            .Bold("Catatan: ").TextBr("URL di atas hanya berfungi untuk Obrolan ini.");

        await telegramService.SendTextMessageAsync(
            htmlMessage.ToString(),
            scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
            includeSenderMessage: true
        );
    }
}