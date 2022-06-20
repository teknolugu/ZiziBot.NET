using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.Services.Extensions;

public static class TelegramServiceRssExtension
{
    public static async Task AddRssUrlAsync(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;
        var fromId = telegramService.FromId;

        var checkUserPermission = await telegramService.CheckUserPermission();

        if (!checkUserPermission)
        {
            Log.Warning("Modify RSS only for admin or private chat!");
            await telegramService.DeleteSenderMessageAsync();
            return;
        }

        var rssUrl = telegramService.GetCommandParamAt<string>(0);

        if (rssUrl.IsNullOrEmpty())
        {
            await telegramService.SendTextMessageAsync("Apa url Feednya?");
            return;
        }

        rssUrl = rssUrl.TryFixRssUrl();
        await telegramService.AppendTextAsync($"URL: {rssUrl}");

        if (!rssUrl.CheckUrlValid())
        {
            await telegramService.AppendTextAsync("Url tersebut sepertinya tidak valid");
            return;
        }

        await telegramService.AppendTextAsync($"Memeriksa RSS Feed");

        var isValid = await rssUrl.IsValidUrlFeed();

        if (!isValid)
        {
            var baseUrl = rssUrl.GetBaseUrl();

            await telegramService.AppendTextAsync("Mencari kemungkinan RSS Feed yang valid");
            var foundUrl = await baseUrl.FindUrlFeed();

            Log.Debug("Found URL Feed: {FoundUrl}", foundUrl);

            if (foundUrl.IsNotNullOrEmpty())
            {
                await telegramService.AppendTextAsync("Menemukan: " + foundUrl);
                rssUrl = foundUrl;
            }
            else
            {
                var notFoundRss = $"Kami tidak dapat memvalidasi {rssUrl} adalah Link RSS yang valid, " +
                                  $"dan mencoba mencari di {baseUrl} tetap tidak dapat menemukan.";

                await telegramService.EditMessageTextAsync(notFoundRss);
                return;
            }
        }

        var rssService = telegramService.GetRequiredService<RssService>();
        var rssFeedService = telegramService.GetRequiredService<RssFeedService>();
        var isFeedExist = await rssService.IsRssExist(chatId, rssUrl);

        Log.Information("Is Url Exist: {IsFeedExist}", isFeedExist);

        if (!isFeedExist)
        {
            await telegramService.AppendTextAsync($"Sedang menyimpan..");

            var data = new Dictionary<string, object>()
            {
                { "url_feed", rssUrl },
                { "chat_id", chatId },
                { "from_id", fromId }
            };

            await rssService.SaveRssSettingAsync(data);

            await telegramService.AppendTextAsync("Memastikan Scheduler sudah berjalan");

            rssFeedService.RegisterUrlFeed(chatId, rssUrl);

            await telegramService.AppendTextAsync($"Tautan berhasil di simpan");
        }
        else
        {
            await telegramService.AppendTextAsync($"Tautan sudah di simpan");
        }
    }

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