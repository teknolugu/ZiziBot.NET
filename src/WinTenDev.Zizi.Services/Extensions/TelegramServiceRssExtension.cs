using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;

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

        await telegramService.AppendTextAsync($"Memeriksa RSS Feed..");

        var isValid = await rssUrl.IsValidUrlFeed();

        if (!isValid)
        {
            var baseUrl = rssUrl.GetBaseUrl();

            await telegramService.AppendTextAsync("Mencari kemungkinan RSS Feed yang valid..", reappendText: true);
            var foundUrl = await baseUrl.FindUrlFeed();

            Log.Debug("Found URL Feed: {FoundUrl}", foundUrl);

            if (foundUrl.IsNotNullOrEmpty())
            {
                await telegramService.AppendTextAsync("Menemukan: " + foundUrl, reappendText: true);
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
            await telegramService.AppendTextAsync($"Sedang menyimpan RSS..", reappendText: true);

            // var data = new Dictionary<string, object>()
            // {
            //     { "url_feed", rssUrl },
            //     { "chat_id", chatId },
            //     { "from_id", fromId }
            // };
            //
            // await rssService.SaveRssSettingAsync(data);

            await rssService.SaveRssSettingAsync(new RssSourceDto
            {
                UserId = fromId,
                ChatId = chatId,
                IsEnabled = true,
                UrlFeed = rssUrl
            });

            await telegramService.AppendTextAsync("Memastikan Scheduler sudah berjalan..", reappendText: true);

            rssFeedService.RegisterUrlFeed(chatId, rssUrl);

            await telegramService.AppendTextAsync($"Tautan berhasil di simpan.", reappendText: true);
        }
        else
        {
            await telegramService.AppendTextAsync($"Tautan sudah di simpan.", reappendText: true);
        }
    }

    public static async Task ImportRssAsync(this TelegramService telegramService)
    {
        var msg = telegramService.Message;
        var msgId = msg.MessageId;
        var chatId = telegramService.ChatId;
        var fromId = telegramService.FromId;
        var dateDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        if (!await telegramService.CheckUserPermission())
        {
            var send = "Maaf, hanya Admin yang dapat mengimport daftar RSS";
            await telegramService.SendTextMessageAsync(send);
            return;
        }

        var rssFeedService = telegramService.GetRequiredService<RssFeedService>();
        var rssService = telegramService.GetRequiredService<RssService>();

        await telegramService.AppendTextAsync("Sedang mempersiapkan");
        var filePath = $"{chatId}/rss-feed_{dateDate}_{msgId}";
        filePath = await telegramService.DownloadFileAsync(filePath);

        await telegramService.AppendTextAsync("Sedang membuka berkas");

        if (filePath.EndsWith(".csv"))
        {
            var csv = filePath.ReadCsv<RssSourceDto>().ToList();

            if (!(telegramService.IsCommand("/gimportrss") &&
                  telegramService.IsFromSudo))
            {
                csv = csv.Select(x => {
                    var newObj = x;
                    newObj.ChatId = chatId;
                    newObj.UserId = fromId;
                    return newObj;
                }).ToList();
            }
            else
            {
                await telegramService.AppendTextAsync("Global Import RSS akan mempertahankan pemilik!", reappendText: true);
            }

            try
            {
                await rssService.ImportRssSettingAsync(csv);

                var htmlMessage = HtmlMessage.Empty
                    .TextBr("RSS berhasil diimport")
                    .Bold("Total: ").CodeBr(csv.Count.ToString());

                await telegramService.AppendTextAsync(htmlMessage.ToString(), reappendCount: 2);
            }
            catch (MongoBulkWriteException<RssSourceEntity> bulkWriteException)
            {
                var writeResult = bulkWriteException.Result;

                var htmlMessage = HtmlMessage.Empty;

                if (writeResult.InsertedCount > 0)
                {
                    htmlMessage.TextBr("RSS berhasil diimport")
                        .Bold("Ditambahkan: ").CodeBr(writeResult.InsertedCount.ToString())
                        .Bold("Dilewat: ").CodeBr(bulkWriteException.WriteErrors.Count.ToString());
                }
                else
                {
                    htmlMessage.TextBr("RSS telah diimport")
                        .Bold("Total: ").CodeBr(bulkWriteException.WriteErrors.Count.ToString());
                }

                await telegramService.AppendTextAsync(htmlMessage.ToString(), reappendCount: 2);
                Log.Error(bulkWriteException, "Error while Bulk Write for Import RSS");
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Error Import RSS");
            }
        }
        else
        {
            var rssLists = await File.ReadAllLinesAsync(filePath);

            foreach (var rssList in rssLists)
            {
                Log.Information("Importing {RssList}", rssList);

                var data = new Dictionary<string, object>()
                {
                    { "url_feed", rssList },
                    { "chat_id", chatId },
                    { "from_id", fromId }
                };

                await rssService.SaveRssSettingAsync(data);
            }

            await telegramService.AppendTextAsync($"Memeriksa RSS duplikat");
            var dedupe = await rssService.DeleteDuplicateAsync();

            var importCount = rssLists.Length;

            if (dedupe != importCount)
            {
                var diff = importCount - dedupe;
                await telegramService.AppendTextAsync($"{diff} RSS berhasil di import");
            }
            else
            {
                await telegramService.AppendTextAsync($"RSS telah di import");
            }
        }

        await rssFeedService.ReRegisterRssFeedByChatId(chatId);
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