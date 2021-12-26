using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CG.Web.MegaApiClient;
using Serilog;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Types;

namespace WinTenDev.Zizi.Services.Externals;

/// <summary>
/// The mega api service class
/// </summary>
public class MegaApiService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MegaApiService"/> class
    /// </summary>
    public MegaApiService()
    {
    }

    /// <summary>
    /// Downloads the file using the specified mega url
    /// </summary>
    /// <param name="megaUrl">The mega url</param>
    /// <param name="onAnswerCallback">The on answer callback</param>
    public async Task DownloadFileAsync(string megaUrl, Func<CallbackAnswer, Task> onAnswerCallback)
    {
        try
        {
            double downloadProgress = 0;
            var swUpdater = Stopwatch.StartNew();
            Log.Information("Starting download from Mega. Url: {0}", megaUrl);
            var client = new MegaApiClient();
            await client.LoginAnonymousAsync();

            var fileLink = new Uri(megaUrl);
            var node = await client.GetNodeFromLinkAsync(fileLink);
            var nodeName = node.Name;

            Log.Debug("Downloading {0}", nodeName);
            IProgress<double> progressHandler = new Progress<double>(async x => {
                downloadProgress = x;

                if (swUpdater.Elapsed.Seconds < 5) return;
                swUpdater.Restart();
                await onAnswerCallback(new CallbackAnswer()
                {
                    CallbackAnswerMode = CallbackAnswerMode.EditMessage,
                    CallbackAnswerText = $"Downloading Progress: {downloadProgress:.##} %"
                });

                // await telegramService.EditMessageTextAsync($"Downloading Progress: {downloadProgress:.##} %");
                // swUpdater.Start();
            });

            await client.DownloadFileAsync(fileLink, nodeName, progressHandler);
        }
        catch (Exception ex)
        {
            await onAnswerCallback(new CallbackAnswer()
            {
                CallbackAnswerMode = CallbackAnswerMode.SendMessage,
                CallbackAnswerText = $"Sesuatu kesalahan telah terjadi."
            });

            // await telegramService.SendTextMessageAsync($"🚫 <b>Sesuatu telah terjadi.</b>\n{ex.Message}");
        }
    }
}