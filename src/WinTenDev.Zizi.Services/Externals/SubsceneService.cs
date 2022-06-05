using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Entities;
using Serilog;
using SerilogTimings;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Entities.MongoDb;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Parsers;

namespace WinTenDev.Zizi.Services.Externals;

public class SubsceneService
{
    private readonly SubsceneConfig _subsceneConfig;
    private readonly ILogger<SubsceneService> _logger;
    private readonly CacheService _cacheService;
    private readonly DatabaseService _databaseService;
    private bool CanUseFeature => _subsceneConfig.IsEnabled;

    public SubsceneService(
        IOptionsSnapshot<SubsceneConfig> subsceneConfig,
        ILogger<SubsceneService> logger,
        CacheService cacheService,
        DatabaseService databaseService
    )
    {
        _subsceneConfig = subsceneConfig.Value;
        _logger = logger;
        _cacheService = cacheService;
        _databaseService = databaseService;
    }

    public async Task<BulkWriteResult<SubsceneMovieItem>> FeedPopularTitles()
    {
        var document = await AnglesharpUtil.DefaultContext.OpenAsync(_subsceneConfig.PopularTitleUrl);
        var htmlTableRows = document.All
            .Where(element => element.NodeName == "TR")
            .Skip(1)
            .OfType<IHtmlTableRowElement>();

        var parsedMovie = htmlTableRows.Select(
            element => {
                var innerText = element.TextContent.Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                var item = new SubsceneMovieItem
                {
                    MovieUrl = element.QuerySelector<IHtmlAnchorElement>("a[href ^= '/subtitles']")?.PathName,
                    Language = innerText.FirstOrDefault(),
                    MovieName = innerText.ElementAtOrDefault(1),
                    Owner = element.Cells.FirstOrDefault(cellElement => cellElement.ClassName == "a5")?.TextContent.Trim(),
                    UploadDate = element.Cells
                        .FirstOrDefault(cellElement => cellElement.ClassName == "a6")
                        ?.Children.FirstOrDefault()?.GetAttribute("title")
                };

                return item;
            }
        );

        await _databaseService.MongoDbOpen("shared");
        var insert = await parsedMovie.InsertAsync();

        return insert;
    }

    public async Task<List<IHtmlAnchorElement>> FeedMovieByTitle(string title)
    {
        var searchUrl = _subsceneConfig.SearchTitleUrl;
        Log.Information("Preparing parse {Url}", searchUrl);
        Log.Debug("Loading web {Url}", searchUrl);
        var searchUrlQuery = searchUrl + "?query=" + title;

        var htmlAnchorElements = await _cacheService.GetOrSetAsync(
            cacheKey: searchUrlQuery,
            staleAfter: "30m",
            action: async () => {
                var document = await AnglesharpUtil.DefaultContext.OpenAsync(searchUrlQuery);
                var querySelectorAll = document.QuerySelectorAll<IHtmlAnchorElement>("a[href ^= '/sub']");

                Log.Debug("Finding download button..");

                return querySelectorAll.ToList();
            }
        );

        await SaveSearchTitle(htmlAnchorElements);

        return htmlAnchorElements;
    }

    public async Task<List<IHtmlAnchorElement>> FeedSubtitleBySlug(string slug)
    {
        var searchSubtitleFileUrl = $"{_subsceneConfig.SearchSubtitleUrl}/{slug}";

        var htmlAnchorElements = await _cacheService.GetOrSetAsync(
            cacheKey: searchSubtitleFileUrl,
            staleAfter: "30m",
            action: async () => {
                var document = await AnglesharpUtil.DefaultContext.OpenAsync(searchSubtitleFileUrl);

                var querySelectorAll = document.QuerySelectorAll<IHtmlAnchorElement>("a[href ^= '/sub']");

                return querySelectorAll.ToList();
            }
        );

        return htmlAnchorElements;
    }

    public async Task<string> GetSubtitleFileAsync(string slug)
    {
        _logger.LogInformation("Preparing download subtitle file {Slug}", slug);

        var address = $"{_subsceneConfig.SearchSubtitleUrl}/{slug}";
        var document = await AnglesharpUtil.DefaultContext.OpenAsync(address);
        var querySelectorAll = document.QuerySelectorAll<IHtmlAnchorElement>("a[href ^= '/subtitles']");
        var subtitleSelector = querySelectorAll.FirstOrDefault(element => element.Href.Contains("text"));
        var subtitleUrl = subtitleSelector!.Href;

        var localPath = "subtitles/" + slug;
        var fileName = slug.Replace("/", "_") + ".zip";

        _logger.LogDebug("Downloading subtitle file. Save to  {Slug}", slug);
        var filePath = await subtitleUrl.MultiThreadDownloadFileAsync(localPath, fileName: fileName);

        _logger.LogInformation("Subtitle file downloaded {Slug}", slug);

        return filePath;
    }

    public async Task SaveSearchTitle(List<IHtmlAnchorElement> searchByTitles)
    {
        _logger.LogInformation("Saving subscene search result. Count: {Count}", searchByTitles.Count);

        try
        {
            await _databaseService.MongoDbOpen("shared");
            if (searchByTitles.Count == 0)
            {
                _logger.LogInformation("No title to save");
                return;
            }

            var subsceneMovieItems = searchByTitles.Select(
                element => new SubsceneMovieItem()
                {
                    MovieName = element.Text,
                    MovieUrl = element.PathName
                }
            );

            await subsceneMovieItems
                .DistinctBy(item => item.MovieUrl)
                .InsertAsync();

            _logger.LogInformation("Saved subscene search result. Count: {Count}", searchByTitles.Count);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error saving subscene search result");
        }
    }

    public async Task<List<SubsceneMovieItem>> GetMovieByTitle(string title)
    {
        var movies = await DB.Find<SubsceneMovieItem>().ManyAsync(
            item =>
                item.MovieName.Contains(title, StringComparison.CurrentCultureIgnoreCase) ||
                item.MovieUrl.Contains(title, StringComparison.CurrentCultureIgnoreCase)
        );

        return movies;
    }

    public async Task<List<SubsceneMovieItem>> GetPopularMovieByTitle()
    {
        var op = Operation.Begin("Get popular Movie by Title");

        var movies = await DB.Find<SubsceneMovieItem>()
            .Sort(item => item.UploadDate, Order.Ascending)
            .ExecuteAsync();

        var popular = movies
            .Select(
                item => new SubsceneMovieItem()
                {
                    MovieName = item.MovieName,
                    MovieUrl = item.MovieUrl,
                    UploadDate = item.UploadDate
                }
            )
            .DistinctBy(item => item.MovieName)
            .OrderByDescending(item => item.UploadDate)
            .ToList();

        _logger.LogInformation("Retrieved popular movies list, an about {Count} item(s)", popular.Count);
        op.Complete();

        return popular;
    }
}
