using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Humanizer;
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

    public async Task<List<SubsceneMovieSearch>> FeedMovieByTitle(string title)
    {
        var searchUrl = _subsceneConfig.SearchTitleUrl;
        Log.Information("Preparing parse {Url}", searchUrl);
        Log.Debug("Loading web {Url}", searchUrl);
        var searchUrlQuery = searchUrl + "?query=" + title;

        var document = await AnglesharpUtil.DefaultContext.OpenAsync(searchUrlQuery);
        var list = document.All
            .FirstOrDefault(element => element.ClassName == "search-result")?.Children
            .Where(element => element.LocalName == "ul")
            .SelectMany(element => element.Children)
            .OfType<IHtmlListItemElement>();

        _logger.LogDebug("Extracting data from {Url}", searchUrlQuery);
        var movieResult = list?.Select(
            element => {
                var movieName = element.TextContent.Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var movieUrl = element.QuerySelector<IHtmlAnchorElement>("a[href ^= '/subtitles']")?.PathName;

                var movie = new SubsceneMovieSearch()
                {
                    MovieName = movieName.FirstOrDefault(),
                    SubtitleCount = movieName.LastOrDefault(),
                    MovieUrl = movieUrl
                };

                return movie;
            }
        ).ToList();

        try
        {
            if (movieResult == null) return default;

            _logger.LogDebug("Saving Subtitle Search to database. {rows} item(s)", movieResult.Count);
            await _databaseService.MongoDbOpen("shared");
            await movieResult.InsertAsync();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Error while inserting movie result");
        }

        return movieResult;
    }

    public async Task<List<SubsceneSubtitleItem>> FeedSubtitleBySlug(string slug)
    {
        var searchSubtitleFileUrl = $"{_subsceneConfig.SearchSubtitleUrl}/{slug}";

        var document = await AnglesharpUtil.DefaultContext.OpenAsync(searchSubtitleFileUrl);
        var list = document.All
            .Where(element => element.LocalName == "tr")
            .Skip(2)
            .OfType<IHtmlTableRowElement>();

        var movieList = list.Select(
                element => {
                    var movieLangAndName = element.Children.FirstOrDefault();
                    var movieNameContents = movieLangAndName?.TextContent.Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    var ownerSubsElement = element.Children.ElementAtOrDefault(3);
                    var commentElement = element.Children.ElementAtOrDefault(4);

                    var item = new SubsceneSubtitleItem()
                    {
                        Language = movieNameContents?.FirstOrDefault(),
                        MovieName = movieNameContents?.LastOrDefault(),
                        MovieUrl = element.QuerySelector<IHtmlAnchorElement>("a[href ^= '/subtitles']")?.PathName,
                        Owner = ownerSubsElement?.TextContent.Trim(),
                        Comment = commentElement?.TextContent.Trim()
                    };

                    return item;
                }
            )
            .Where(item => item.Language != null)
            .ToList();

        try
        {
            _logger.LogDebug("Saving Subtitle language item Search to database. {rows} item(s)", movieList.Count);
            await _databaseService.MongoDbOpen("shared");
            var insertResult = await movieList.InsertAsync();
            _logger.LogDebug("Insert subtitle lang. Result: {rows}", insertResult);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Error while inserting movie result");
        }

        return movieList;
    }

    public async Task<SubsceneMovieDetail> GetSubtitleFileAsync(string slug)
    {
        _logger.LogInformation("Preparing download subtitle file {Slug}", slug);

        var address = $"{_subsceneConfig.SearchSubtitleUrl}/{slug}";
        var document = await AnglesharpUtil.DefaultContext.OpenAsync(address);
        var all = document.All
            .Where(element => element.ClassName == "top left")
            .OfType<IHtmlDivElement>()
            .FirstOrDefault();

        var subtitleListUrl = "/subtitles/" + slug;
        var language = slug.Split("/").ElementAtOrDefault(1);
        var posterElement = (all?.QuerySelector<IHtmlAnchorElement>("a[href]")?.Children.FirstOrDefault() as IHtmlImageElement)?.Source;
        var headerElement = all?.QuerySelector<IHtmlDivElement>("div.header");
        var movieTitle = ((headerElement?.Children.FirstOrDefault() as IHtmlHeadingElement)?.Children.FirstOrDefault() as IHtmlSpanElement)?.TextContent.Trim();
        var releaseInfo = headerElement?.QuerySelector<IHtmlListItemElement>("li.release")?.Children.OfType<IHtmlDivElement>()
            .Select(element => element.TextContent.Trim()).ToList();
        var authorElement = headerElement?.QuerySelector<IHtmlAnchorElement>("a[href ^= '/u']");
        var comment = headerElement?.QuerySelector<IHtmlDivElement>("div.comment");
        var subtitleUrl = document.QuerySelectorAll<IHtmlAnchorElement>("a[href ^= '/subtitles']")
            .FirstOrDefault(element => element.Href.Contains("text"))?.Href;

        var movieDetail = new SubsceneMovieDetail()
        {
            SubtitleMovieUrl = subtitleListUrl,
            MovieName = movieTitle,
            Language = language.Titleize(),
            CommentaryUrl = authorElement?.PathName,
            CommentaryUser = authorElement?.TextContent.Trim(),
            PosterUrl = posterElement,
            ReleaseInfo = releaseInfo?.JoinStr("\n"),
            ReleaseInfos = releaseInfo,
            Comment = comment?.TextContent.Trim(),
            SubtitleDownloadUrl = subtitleUrl
        };

        // var localPath = "subtitles/" + slug;
        // var fileName = releaseInfo?.FirstOrDefault() + ".zip";
        //
        // _logger.LogDebug("Downloading subtitle file. Save to  {Slug}", slug);
        // var filePath = await subtitleUrl.MultiThreadDownloadFileAsync(localPath, fileName: fileName);
        //
        // movieDetail.LocalFilePath = filePath;

        return movieDetail;
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

    public async Task<List<SubsceneMovieSearch>> GetMovieByTitle(string title)
    {
        var movies = await DB.Find<SubsceneMovieSearch>().ManyAsync(
            item =>
                item.MovieName.Contains(title) ||
                item.MovieUrl.Contains(title)
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

    public async Task<List<SubsceneSubtitleItem>> GetSubtitleBySlug(string slug)
    {
        var subtitles = await DB.Find<SubsceneSubtitleItem>()
            .ManyAsync(
                item =>
                    new ExpressionFilterDefinition<SubsceneSubtitleItem>(
                        subtitleItem =>
                            subtitleItem.MovieUrl.Contains(slug)
                    )
            );

        return subtitles;
    }

    public async Task<List<SubsceneMovieSearch>> GetOrFeedMovieByTitle(string title)
    {
        var getMovieByTitle = await GetMovieByTitle(title);

        if (getMovieByTitle.Count > 0)
        {
            await FeedMovieByTitle(title);

            return getMovieByTitle;
        }

        var feedMovieByTitle = await FeedMovieByTitle(title);

        return feedMovieByTitle;
    }

    public async Task<List<SubsceneSubtitleItem>> GetOrFeedSubtitleBySlug(string slug)
    {
        var movieBySlug = await GetSubtitleBySlug(slug);

        if (movieBySlug.Count > 0)
        {
            await FeedSubtitleBySlug(slug);

            return movieBySlug;
        }

        var feedSubtitleBySlug = await FeedSubtitleBySlug(slug);

        return feedSubtitleBySlug;
    }
}
