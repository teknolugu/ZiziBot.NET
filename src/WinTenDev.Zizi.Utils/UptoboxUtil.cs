using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Serilog;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Models.Types.Uptobox;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.Zizi.Utils;

public static class UptoboxUtil
{
    private const string UptoboxApi = "https://uptobox.com/api";

    public static bool IsUptoboxUrl(this string url)
    {
        if (!url.IsValidUrl()) return false;

        var uri = new Uri(url);
        return "uptobox.com".Contains(uri.Host, StringComparison.InvariantCultureIgnoreCase);
    }

    public static async Task<UptoboxUser> GetMe()
    {
        Log.Information("Getting Uptobox Me..");
        var token = "BotSettings.UptoboxToken";
        if (token.IsNullOrEmpty())
        {
            Log.Information("Uptobox disabled because Token is not configured.");
            return null;
        }

        var url = Url.Combine(UptoboxApi, "user/me");
        var json = await url
            .SetQueryParam("token", token)
            .GetJsonAsync<UptoboxUser>();
        // Log.Debug("Uptobox Me: {0}", json.ToJson(true));

        return json;
    }

    public static async Task<string> GetDownloadLinkAsync(string fileId)
    {
        Log.Information("Getting download Link. FileID: {0}", fileId);
        var url = Url.Combine(UptoboxApi, "link");
        var token = "BotSettings.UptoboxToken";

        var req = url
            .SetQueryParam("token", token)
            .SetQueryParam("file_code", fileId);

        var waiting = await req.GetJsonAsync<UptoboxLink>();
        Log.Debug("Waiting: {0}", waiting.ToJson(true));

        return waiting.LinkData.DlLink;
    }

    public static async Task<string> DownloadUrlAsync(
        long chatId,
        string url,
        Func<CallbackAnswer, Task> answer,
        bool withoutDownload = false
    )
    {
        try
        {
            Log.Information("Starting download from Uptobox. Url: {0}", url);

            var user = await GetMe();
            var isPremium = user.UserData.Premium;
            if (!isPremium)
            {
                Log.Information("Uptobox can't be continue because Token isn't premium.");
                return null;
            }

            var fileId = url.Replace("https://uptobox.com/", "", StringComparison.InvariantCulture).Trim();
            var downloadLink = await GetDownloadLinkAsync(fileId);

            if (!withoutDownload)
            {
                // await telegramService.DownloadFile(downloadLink);
            }

            return downloadLink;
        }
        catch
        {
            await answer(new CallbackAnswer()
            {
                CallbackAnswerText = "Terjadi kesalahan ketika mengunduh file dari Uptobox. Pastikan kembali tautan.",
                CallbackAnswerModes = new List<CallbackAnswerMode>()
                {
                    CallbackAnswerMode.EditMessage
                }
            });

            return null;
        }
    }
}