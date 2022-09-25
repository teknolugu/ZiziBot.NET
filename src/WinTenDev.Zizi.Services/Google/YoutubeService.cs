using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace WinTenDev.Zizi.Services.Google;

public class YoutubeService
{
    private readonly ILogger<YoutubeService> _logger;
    private readonly CacheService _cacheService;
    private readonly YoutubeClient _youtubeClient;

    public YoutubeService(
        ILogger<YoutubeService> logger,
        CacheService cacheService
    )
    {
        _logger = logger;
        _cacheService = cacheService;
        _youtubeClient = new YoutubeClient();
    }

    public async Task<IReadOnlyList<ISearchResult>> SearchByTitle(
        string keyword,
        int limit = 50
    )
    {
        var searchResults = await _cacheService.GetOrSetAsync(
            cacheKey: "youtube_search_" + keyword,
            action: async () => {
                var searchResults = await _youtubeClient.Search
                    .GetResultsAsync(keyword)
                    .CollectAsync(limit);

                return searchResults;
            });

        return searchResults;
    }

    public async Task<IReadOnlyList<VideoSearchResult>> SearchVideoByTitle(
        string keyword,
        int limit = 50
    )
    {
        var searchResults = await _cacheService.GetOrSetAsync(
            cacheKey: "youtube_search-video_" + keyword,
            action: async () => {
                var searchResults = await _youtubeClient.Search
                    .GetVideosAsync(keyword)
                    .CollectAsync(limit);
                return searchResults;
            });

        return searchResults;
    }

    public async Task<StreamManifest> GetStreamManifestAsync(string videoUrl)
    {
        var streamManifest = await _cacheService.GetOrSetAsync(
            cacheKey: "youtube_manifest_" + videoUrl.ToCacheKey(),
            staleAfter: "1h",
            action: async () => {
                var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoUrl);
                return streamManifest;
            });

        return streamManifest;
    }

    public async Task<Video> GetVideoManifestAsync(string videoUrl)
    {
        var streamManifest = await _cacheService.GetOrSetAsync(
            cacheKey: "youtube_manifest_" + videoUrl.ToCacheKey(),
            staleAfter: "1h",
            action: async () => {
                var streamManifest = await _youtubeClient.Videos.GetAsync(videoUrl);
                return streamManifest;
            });

        return streamManifest;
    }

    public async Task<Stream> GetStreamAsync(IStreamInfo streamInfo)
    {
        var streamManifest = await _youtubeClient.Videos.Streams.GetAsync(streamInfo);
        return streamManifest;
    }

    public async Task<string> DownloadAsync(
        IStreamInfo streamInfo,
        string fileName
    )
    {
        var videoUrl = streamInfo.Url;
        _logger.LogInformation("Downloading: {0}", videoUrl);

        var filePath = Path.Combine("Storage", "Temp", "Youtube", "Downloads", fileName).EnsureDirectory();
        await _youtubeClient.Videos.Streams.DownloadAsync(streamInfo, filePath);

        return filePath;
    }
}