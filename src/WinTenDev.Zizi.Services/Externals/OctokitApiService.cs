using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MoreLinq;
using Octokit;
using Serilog;
using SerilogTimings;
using Telegram.Bot.Types;
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

    public async Task<GithubRepoInfo> GetGithubRepoInfoAsync(string url)
    {
        var urlSplit = url.Split("/");
        var repoOwner = urlSplit.ElementAtOrDefault(3);
        var repoName = urlSplit.ElementAtOrDefault(4);
        var repoUrl = urlSplit.Take(5).JoinStr("/");

        var githubRepoInfo = await _cacheService.GetOrSetAsync(
            cacheKey: repoUrl,
            staleAfter: "5m",
            action: async () => {
                var client = CreateClient();
                var op = Operation.Begin("Getting Github Repo Info. Url: {Url}", url);

                var repositoryClient = client.Repository;
                var issues = await client.Issue.GetAllForRepository(repoOwner, repoName);
                var repository = await repositoryClient.Get(repoOwner, repoName);
                var releaseAll = await repositoryClient.Release.GetAll(repoOwner, repoName);
                var commits = await repositoryClient.Commit.GetAll(repoOwner, repoName);
                var pullRequests = await repositoryClient.PullRequest.GetAllForRepository(repoOwner, repoName);

                var githubRepoInfo = new GithubRepoInfo()
                {
                    Owner = repoOwner,
                    Name = repoName,
                    CreatedAt = repository.CreatedAt,
                    UpdatedAt = repository.UpdatedAt,
                    PushedAt = repository.PushedAt,
                    FullName = repository.FullName,
                    Url = repository.HtmlUrl,
                    Homepage = repository.Homepage,
                    Description = repository.Description,
                    Language = repository.Language,
                    Topics = repository.Topics,
                    StargazersCount = repository.StargazersCount,
                    WatchersCount = repository.WatchersCount,
                    ForksCount = repository.ForksCount,
                    License = repository.License,
                    Releases = releaseAll,
                    Commits = commits,
                    PullRequests = pullRequests,
                    Issues = issues
                };

                op.Complete();

                return githubRepoInfo;
            }
        );

        return githubRepoInfo;
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
        var repoInfo = await GetGithubRepoInfoAsync(url);
        var releaseList = repoInfo.Releases;

        var latestRelease = releaseList.FirstOrDefault(release => !release.Draft);

        if (latestRelease == null) return null;

        var allAssets = latestRelease.Assets;

        var htmlMessage = HtmlMessage.Empty;

        htmlMessage
            .Url(repoInfo.Url, repoInfo.FullName).Br()
            .Url(latestRelease.HtmlUrl, latestRelease.Name).Br()
            .Bold("By ").CodeBr(repoInfo.Owner).Br();

        if (allAssets.Count == 0)
        {
            htmlMessage.TextBr("No assets found.");
            return htmlMessage;
        }

        allAssets.ForEach(
            (
                asset,
                index
            ) => {
                var urlDoc = asset.BrowserDownloadUrl;
                var assetSize = asset.Size.ToInt64().SizeFormat();

                htmlMessage.Text($"{index + 1}. ").Url(urlDoc, asset.Name)
                    .Text($"\n└ ⬇ {assetSize} - {asset.CreatedAt}")
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