using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Octokit;
using Telegram.Bot.Types;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;
using FileMode=System.IO.FileMode;

namespace WinTenDev.Zizi.Services.Externals;

public class OctokitApiService
{
    private readonly OctokitConfig _githubConfig;
    private readonly CacheService _cacheService;

    public OctokitApiService(
        IOptionsSnapshot<OctokitConfig> githubConfig,
        CacheService cacheService
    )
    {
        _githubConfig = githubConfig.Value;
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

    public async Task<List<IAlbumInputMedia>> GetLatestReleaseAssets(string url)
    {
        var listAlbum = new List<IAlbumInputMedia>();

        var releaseAll = await GetGithubReleaseAssets(url);
        var latestRelease = releaseAll.FirstOrDefault();

        if (latestRelease == null) return null;

        foreach (var asset in latestRelease.Assets)
        {
            var urlDoc = asset.BrowserDownloadUrl;
            var savedFile = await urlDoc.MultiThreadDownloadFileAsync();
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

        return listAlbum;
    }
}