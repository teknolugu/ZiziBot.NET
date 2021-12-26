using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Serilog;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Types;

namespace WinTenDev.Zizi.Utils;

public static class Transfer
{
    public static void AddQueue()
    {
    }

    public static async Task<string> DownloadFile(long chatId, string address, Func<CallbackAnswer, Task> answer)
    {
        try
        {
            var totalSize = "";
            var wc = new WebClient();
            var swUpdater = new Stopwatch();
            var swProgress = new Stopwatch();
            var uri = new Uri(address);

            var cachePath = BotSettings.PathCache;
            // var url = new Url(address.GetAutoRedirectedUrl());
            var url = address.GetAutoRedirectedUrl();
            var fileName = Path.GetFileName(url.AbsoluteUri);
            var saveFilePath = Path.Combine(cachePath, chatId.ToString(), fileName);

            swUpdater.Start();
            swProgress.Start();

            wc.DownloadFileAsync(uri, saveFilePath);


            wc.DownloadProgressChanged += async (sender, args) => {
                if (swUpdater.Elapsed.Seconds < 5) return;

                swUpdater.Reset();
                var progressPercentage = args.ProgressPercentage;
                var downloadSpeed = (args.BytesReceived / swProgress.Elapsed.TotalSeconds).SizeFormat("/s");
                totalSize = args.TotalBytesToReceive.ToDouble().SizeFormat();
                var progressText = "Downloading file to Temp" +
                                   $"\n<b>Url:</b> {url.AbsoluteUri}" +
                                   $"\n<b>Referrer:</b> {address}" +
                                   $"\n<b>Name:</b> {fileName}" +
                                   $"\n<b>Progress:</b> {progressPercentage} %" +
                                   $"\n<b>Size:</b> {totalSize}" +
                                   $"\n<b>Speed:</b> {downloadSpeed}";

                await answer(new CallbackAnswer()
                {
                    CallbackAnswerText = progressText,
                    CallbackAnswerModes = new[]
                    {
                        CallbackAnswerMode.EditMessage
                    }
                });

                swUpdater.Start();
            };

            wc.DownloadFileCompleted += async (sender, args) => {
                var completeText = "Download  complete" +
                                   $"\n<b>Url:</b> {address}" +
                                   $"\n<b>Size:</b> {totalSize}" +
                                   $"\n<b>Success:</b> {!args.Cancelled}";

                await answer(new CallbackAnswer()
                {
                    CallbackAnswerText = completeText,
                    CallbackAnswerModes = new[]
                    {
                        CallbackAnswerMode.EditMessage
                    }
                });

                Log.Information("Download file complete {Cancelled}", args.Cancelled);

                var preparingUpload = "Preparing upload file to Google Drive." +
                                      $"\nFile: {fileName}";

                await answer(new CallbackAnswer()
                {
                    CallbackAnswerText = preparingUpload,
                    CallbackAnswerModes = new[]
                    {
                        CallbackAnswerMode.EditMessage
                    }
                });

                // telegramService.UploadFile(saveFilePath);
            };

            wc.Dispose();
            return saveFilePath;
        }

        catch (Exception e)
        {
            Log.Error(e.Demystify(), "Error when starting download");
            await answer(new CallbackAnswer()
            {
                CallbackAnswerText = $"⛔️ Error when download file. \n{e.Message}",
                CallbackAnswerModes = new[]
                {
                    CallbackAnswerMode.EditMessage
                }
            });
        }

        return null;
    }
}