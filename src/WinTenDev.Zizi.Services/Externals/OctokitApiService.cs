using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MoreLinq;
using Octokit;
using Serilog;
using Telegram.Bot.Types;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;
using FileMode=System.IO.FileMode;

namespace WinTenDev.Zizi.Services.Externals;

public class OctokitApiService
{
    private readonly OctokitConfig _githubConfig;
    private readonly RssFeedConfig _rssFeedConfig;
    private readonly CacheService _cacheService;

    public OctokitApiService(
        IOptionsSnapshot<OctokitConfig> githubConfig,
        IOptionsSnapshot<RssFeedConfig> rssFeedConfig,
        CacheService cacheService
    )
    {
        _githubConfig = githubConfig.Value;
        _rssFeedConfig = rssFeedConfig.Value;
        _cacheService = cacheService;
    }

    private GitHubClient CreateClient()
    {
        var client = new GitHubClient(new ProductHeaderValue(_githubConfig.ProductHeaderName))
        {
            Credentials = new Credentials(_githubConfig.AccessToken)
        };

        return client;
    }

    public async Task<IReadOnlyList<Release>> GetGithubReleaseAssets(string url)
    {
        var urlSplit = url.Split("/");
        var repoOwner = urlSplit.ElementAtOrDefault(3);
        var repoName = urlSplit.ElementAtOrDefault(4);

        var githubReleaseAll = await _cacheService.GetOrSetAsync(
            cacheKey: url,
            action: async () => {
                var githubReleaseAll = await CreateClient()
                    .Repository.Release.GetAll(repoOwner, repoName);

                return githubReleaseAll;
            }
        );

        return githubReleaseAll;
    }

    public async Task<HtmlMessage> GetLatestReleaseAssetsList(string url)
    {
        var releaseAll = await GetGithubReleaseAssets(url);
        var latestRelease = releaseAll.FirstOrDefault();

        if (latestRelease == null) return null;

        var maxAttachmentSize = _rssFeedConfig.MaxAttachmentSize.ToeByteSize();

        var allAssets = latestRelease.Assets;
        var filteredAssets = allAssets
            .Where(
                releaseAsset =>
                    releaseAsset.Size <= maxAttachmentSize.Bytes
            ).ToList();

        Log.Information(
            "Found filtered assets {FilteredAssets} of {AllAssets} for URL: {Url}",
            filteredAssets.Count,
            allAssets.Count,
            url
        );

        var htmlMessage = HtmlMessage.Empty;

        htmlMessage
            .Url(latestRelease.HtmlUrl, latestRelease.Name).Br()
            .Bold("Author: ").CodeBr(latestRelease.Author.Login).Br();

        if (allAssets.Count == 0) return htmlMessage;

        allAssets.ForEach(
            (
                asset,
                index
            ) => {
                var urlDoc = asset.BrowserDownloadUrl;
                var assetSize = asset.Size.ToInt64().SizeFormat();

                htmlMessage.Text($"{index + 1}. ").Url(urlDoc, asset.Name)
                    .Text($"\n└ ⬇ {assetSize} - {asset.DownloadCount} downloads")
                    .Br();
            }
        );

        return htmlMessage;
    }

    public async Task<List<IAlbumInputMedia>> GetLatestReleaseAssets(
        string url,
        string tempDir
    )
    {
        var listAlbum = new List<IAlbumInputMedia>();

        var releaseAll = await GetGithubReleaseAssets(url);
        var latestRelease = releaseAll.FirstOrDefault();

        if (latestRelease == null) return null;

        var maxAttachmentSize = _rssFeedConfig.MaxAttachmentSize.ToeByteSize();

        var allAssets = latestRelease.Assets;
        var filteredAssets = allAssets
            .Where(
                releaseAsset =>
                    releaseAsset.Size <= maxAttachmentSize.Bytes
            ).ToList();

        Log.Information(
            "Found filtered assets {FilteredAssets} of {AllAssets} for URL: {Url}",
            filteredAssets.Count,
            allAssets.Count,
            url
        );

        foreach (var asset in filteredAssets)
        {
            var urlDoc = asset.BrowserDownloadUrl;
            var savedFile = await urlDoc.MultiThreadDownloadFileAsync(tempDir);
            var fileName = Path.GetFileName(savedFile);

            var fileStream = new FileStream(
                savedFile,
                FileMode.Open,
                FileAccess.Read
            );

            listAlbum.Add(
                new InputMediaDocument(
                    new InputMedia(fileStream, fileName)
                    {
                        FileName = asset.Name
                    }
                )
                {
                    Caption = asset.Name
                }
            );
        }

        return listAlbum.Take(10).ToList();
    }
}
