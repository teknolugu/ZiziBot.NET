using System;
using System.Threading.Tasks;
using Serilog;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.Zizi.Services.Extensions
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

        public static async Task RssPullAsync(this TelegramService telegramService)
        {
            var chatId = telegramService.ChatId;

            var checkUserPermission = await telegramService.CheckUserPermission();
            if (!checkUserPermission)
            {
                Log.Warning("You must Admin or Private chat");

                return;
            }

            var jobsService = telegramService.GetRequiredService<JobsService>();

            Log.Information("Pulling RSS in {0}", chatId);

 #pragma warning disable CS4014
            Task.Run(
 #pragma warning restore CS4014
                async () => {
                    await telegramService.SendTextMessageAsync("Sedang menjalankan Trigger RSS Job..");

                    var reducedChatId = telegramService.ReducedChatId;
                    var recurringId = $"rss-{reducedChatId}";
                    jobsService.TriggerJobsByPrefix(recurringId);

                    await telegramService.EditMessageTextAsync(
                        sendText: "RSS Jobs untuk Obrolan ini berhasil dipicu, artikel baru akan segera masuk jika tersedia.",
                        scheduleDeleteAt: DateTime.UtcNow.AddMinutes(2),
                        includeSenderMessage: true
                    );
                }
            );
        }
    }
}