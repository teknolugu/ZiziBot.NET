using System;
using System.Threading.Tasks;

namespace WinTenDev.Zizi.Services.Telegram.Extensions
{
    public static class TelegramServiceRssExtension
    {
        public static async Task GetRssControlAsync(this TelegramService telegramService)
        {
            var chatId = telegramService.ChatId;
            var chatTitle = telegramService.ChatTitle;

            await telegramService.DeleteSenderMessageAsync();

            if (!await telegramService.CheckUserPermission()) return;

            await telegramService.SendTextMessageAsync("Sedang mengambil RSS..", replyToMsgId: 0);

            var buttonMarkup = await telegramService.RssService.GetButtonMarkup(chatId);

            var messageText = buttonMarkup == null
                ? "Sepertinya tidak ada RSS di obrolan ini"
                : $"RSS Control for {chatTitle}" +
                  "\nHalaman 1";

            await telegramService.EditMessageTextAsync(
                messageText,
                buttonMarkup,
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(10),
                includeSenderMessage: true,
                preventDuplicateSend: true
            );
        }
    }
}