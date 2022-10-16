using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DebounceThrottle;
using Downloader;
using Flurl;
using Flurl.Http;
using Serilog;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils.IO;

namespace WinTenDev.Zizi.Utils;

public static class UrlUtil
{
    public static Uri GenerateUrlQrApi(this string data)
    {
        var baseUrl = "https://api.qrserver.com";
        var strData = Url.Encode(data);
        return new Uri($"{baseUrl}/v1/create-qr-code/?size=300x300&margin=10&data={strData}");
    }

    public static async Task<List<GoQrReadResult>> ReadQrCodeAsync(this string filePath)
    {
        var baseUrl = "https://api.qrserver.com";
        var request = await baseUrl
            .AppendPathSegment("/v1/read-qr-code/")
            .PostMultipartAsync(
                content => content
                    .AddFile("file", filePath)
            );

        return await request.GetJsonAsync<List<GoQrReadResult>>();
    }

    public static void SaveUrlTo(
        this string remoteFileUrl,
        string localFileName
    )
    {
        var webClient = new WebClient();

        Log.Information(
            "Saving {RemoteFileUrl} to {LocalFileName}",
            remoteFileUrl,
            localFileName
        );
        webClient.DownloadFile(remoteFileUrl, localFileName);
        webClient.Dispose();
    }

    public static async Task<string> DownloadFileAsync(this string url)
    {
        var paths = Path.Combine("Storage/Caches/");
        var saved = await url
            .WithAutoRedirect(true)
            .DownloadFileAsync(paths);

        return saved;
    }

    public static async Task<string> MultiThreadDownloadFileAsync(
        this string url,
        string tempDir,
        string fileName = ""
    )
    {
        var throttleDispatcher = new ThrottleDispatcher(2000);

        var pathFileName = Path.GetFileName(url);
        if (fileName.IsNotNullOrEmpty()) pathFileName = fileName;

        var paths = Path.Combine(
            "Storage/Caches/",
            tempDir,
            pathFileName
        );

        var downloadOpt = new DownloadConfiguration()
        {
            ChunkCount = 8,
            ParallelDownload = true,
            MaxTryAgainOnFailover = 10,
            OnTheFlyDownload = false,
            RequestConfiguration = // config and customize request headers
            {
                Accept = "*/*",
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                CookieContainer = new CookieContainer(),// Add your cookies
                Headers = new WebHeaderCollection(),// Add your custom headers
                KeepAlive = false,// default value is false
                ProtocolVersion = HttpVersion.Version11,// Default value is HTTP 1.1
                UseDefaultCredentials = false,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.64 Safari/537.36 Edg/101.0.1210.53"
            }
        };

        var downloader = new DownloadService(downloadOpt);

        downloader.DownloadStarted += (
            sender,
            args
        ) => {
            Log.Information(
                "Downloading File. FileName: {FileName}, Size: {Size}",
                args.FileName,
                args.TotalBytesToReceive
            );
        };

        downloader.DownloadProgressChanged += (
            sender,
            args
        ) => {
            var downloadService = sender as DownloadService;
            if (downloadService == null) return;

            var downloadPackage = downloadService.Package;
            throttleDispatcher.Throttle(
                () => {
                    Log.Debug(
                        "Downloading URL: {FileName}. {Run}/{Size} - {Speed} ({Progress}%)",
                        downloadPackage.Address,
                        downloadPackage.ReceivedBytesSize.SizeFormat(),
                        downloadPackage.TotalFileSize.SizeFormat(),
                        args.AverageBytesPerSecondSpeed.SizeFormat("/s"),
                        args.ProgressPercentage.ToString("N2")
                    );
                }
            );
        };

        downloader.DownloadFileCompleted += (
            sender,
            args
        ) => {
            var downloadService = sender as DownloadService;
            if (downloadService == null) return;

            var downloadPackage = downloadService.Package;

            Log.Information(
                "Download completed. Url: {Address}. Size: {Size}",
                downloadPackage.Address,
                downloadPackage.TotalFileSize.SizeFormat()
            );
        };

        await downloader.DownloadFileTaskAsync(url, paths);

        return paths;
    }

    public static string SaveToCache(
        this string remoteFileUrl,
        string localFileName
    )
    {
        var webClient = new WebClient();

        var cachePath = Path.Combine("Storage", "Caches");
        var localPath = Path.Combine(cachePath, localFileName).SanitizeSlash().EnsureDirectory();

        Log.Debug(
            "Saving {RemoteFileUrl} to {LocalPath}",
            remoteFileUrl,
            localPath
        );
        webClient.DownloadFile(remoteFileUrl, localPath);
        webClient.Dispose();

        return localPath;
    }

    public static async Task<string> SaveToCacheAsync(
        this string remoteFileUrl,
        string localFileName
    )
    {
        var cachePath = Path.Combine("Storage", "Caches");
        var localPath = Path.Combine(cachePath, localFileName).SanitizeSlash().EnsureDirectory();

        Log.Debug(
            "Saving {RemoteFileUrl} to {LocalPath}",
            remoteFileUrl,
            localPath
        );
        await remoteFileUrl.DownloadFileAsync(remoteFileUrl, localPath);

        return localPath;
    }

    public static Url ParseUrl(this string urlPath)
    {
        var url = new Url(urlPath);

        return url;
    }

    public static bool IsValidUrl(this string urlPath)
    {
        return Url.IsValid(urlPath);
    }

    public static async Task<bool> IsExistUrl(this string url)
    {
        try
        {
            var head = await url
                .AllowAnyHttpStatus()
                .HeadAsync();

            var isExist = head.StatusCode is >= 200 and < 500;

            return isExist;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public static bool IsUrlRedirected(this string url)
    {
        var req = (HttpWebRequest)HttpWebRequest.Create(url);
        req.Method = "HEAD";
        req.AllowAutoRedirect = false;
        var resp = (HttpWebResponse)req.GetResponse();

        return resp.StatusCode == HttpStatusCode.Redirect ||
               resp.StatusCode == HttpStatusCode.MovedPermanently ||
               resp.StatusCode == HttpStatusCode.RedirectKeepVerb ||
               resp.StatusCode == HttpStatusCode.RedirectMethod;
    }

    public static string GetRedirectedUrl(this string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        Log.Information("Trying get redirected url: {Url}", url);
        var maxRedirCount = 8;// prevent infinite loops
        var newUrl = url;
        do
        {
            HttpWebRequest webRequest = null;
            HttpWebResponse webResponse = null;
            try
            {
                webRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                webRequest.Method = "HEAD";
                webRequest.AllowAutoRedirect = true;
                webResponse = (HttpWebResponse)webRequest.GetResponse();
                var statusCode = (int)webResponse.StatusCode;
                Log.Debug("Response: {@Response}", webResponse);

                if (statusCode >= 300 &&
                    statusCode <= 399)
                {
                    Log.Debug("Getting redirected from Headers");

                    // newUrl = webResponse.Headers["Location"];
                    if (newUrl == null)
                        return url;

                    if (newUrl.IndexOf("://", StringComparison.Ordinal) == -1)
                    {
                        // Doesn't have a URL Schema, meaning it's a relative or absolute URL
                        var u = new Uri(new Uri(url), newUrl);
                        newUrl = u.ToString();
                    }

                    Log.Debug("Finish redirect");
                }

                newUrl = webResponse.ResponseUri.AbsoluteUri;

                // switch (resp.StatusCode)
                // {
                //     case HttpStatusCode.OK:
                //         return newUrl;
                //     case HttpStatusCode.Redirect:
                //     case HttpStatusCode.MovedPermanently:
                //     case HttpStatusCode.RedirectKeepVerb:
                //     case HttpStatusCode.RedirectMethod:
                //         Log.Debug("Getting redirected from Headers");
                //
                //         newUrl = resp.Headers["Location"];
                //         if (newUrl == null)
                //             return url;
                //
                //         if (newUrl.IndexOf("://", System.StringComparison.Ordinal) == -1)
                //         {
                //             // Doesn't have a URL Schema, meaning it's a relative or absolute URL
                //             Uri u = new Uri(new Uri(url), newUrl);
                //             newUrl = u.ToString();
                //         }
                //
                //         Log.Debug("Finish get.");
                //         break;
                //     default:
                //         return newUrl;
                // }

                url = newUrl;
            }
            catch (WebException ex)
            {
                // Return the last known good URL
                Log.Error(ex.Demystify(), "Error WebException when try get redirectedUrl");
                return newUrl;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "Error when try get redirectedUrl");
                return null;
            }
            finally
            {
                if (webResponse != null)
                    webResponse.Close();
            }
        } while (maxRedirCount-- > 0);

        Log.Debug("Redirected URL: {Url}", url);
        return url;
    }

    public static Uri GetAutoRedirectedUrl(this string url)
    {
        if (!IsNeedRedirect(url)) return new Uri(url);

        var webRequest = (HttpWebRequest)HttpWebRequest.Create(url);
        webRequest.Method = "HEAD";
        webRequest.AllowAutoRedirect = true;
        var webResponse = (HttpWebResponse)webRequest.GetResponse();
        Log.Debug("Response: {@WebResponse}", webResponse);

        return webResponse.ResponseUri;
    }

    public static bool IsNeedRedirect(string url)
    {
        var uri = new Uri(url);
        Log.Debug("Uri: {Host}", uri);

        var skipRedirect = new List<string>()
        {
            "soft98.ir"
        };

        var needRedirect = false;
        var host = uri.Host;
        foreach (var skip in skipRedirect)
        {
            if (!host.Contains(skip, StringComparison.CurrentCulture))
            {
                needRedirect = true;
            }
        }

        Log.Debug(
            "Is {Host} need redirect {NeedRedirect}",
            host,
            needRedirect
        );
        return needRedirect;
    }

    // public static bool MakeRequest(Uri url)
    // {
    //     using var client = new HttpClient(new HttpClientHandler()
    //     {
    //         AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
    //     }) { Timeout = TimeSpan.FromSeconds(30) };
    //     var request = new HttpRequestMessage()
    //     {
    //         RequestUri = url,
    //         Method = HttpMethod.Get
    //     };
    //
    //     HttpResponseMessage response = client.SendAsync(request).Result;
    //     var statusCode = (int)response.StatusCode;
    //
    //     // We want to handle redirects ourselves so that we can determine the final redirect Location (via header)
    //     if (statusCode >= 300 && statusCode <= 399)
    //     {
    //         var redirectUri = response.Headers.Location;
    //         if (!redirectUri.IsAbsoluteUri)
    //         {
    //             redirectUri = new Uri(request.RequestUri.GetLeftPart(UriPartial.Authority) + redirectUri);
    //         }
    //         _status.AddStatus($"Redirecting to {redirectUri}");
    //         return MakeRequest(redirectUri);
    //     }
    //     else if (!response.IsSuccessStatusCode)
    //     {
    //         throw new Exception();
    //     }
    //
    //     return true;
    // }
    public static bool IsMegaUrl(this string url)
    {
        if (!url.IsValidUrl()) return false;

        var uri = new Uri(url);
        return uri.Host.Contains("mega.nz");
    }

    public static async Task<string> GetServerFileName(this string url)
    {
        string filename;

        try
        {
            var response = await url
                .OpenFlurlSession()
                .GetAsync();

            filename = response.Headers
                .FirstOrDefault("content-disposition").Replace("\"", "")
                .Split(";").LastOrDefault()?
                .Split("=").LastOrDefault();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error when try get server file name");
            filename = StringUtil.NewGuid();
        }

        Log.Debug(
            "Server file name: {Filename} for Url: {Url}",
            filename,
            url
        );

        return filename;
    }

    public static bool IsBaseUrl(this string url)
    {
        var baseUrl = url.GetBaseUrl();
        var isBaseUrl = baseUrl == url;

        return isBaseUrl;
    }

    public static string ToCacheKey(this string url)
    {
        var key = url
            .Replace("https://", "")
            .Replace("http:/", "")
            .Replace("/", "_")
            .TrimEnd('_');

        return key;
    }
}