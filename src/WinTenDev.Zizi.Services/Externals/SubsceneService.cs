using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Microsoft.Extensions.Options;
using Serilog;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Parsers;

namespace WinTenDev.Zizi.Services.Externals;

public class SubsceneService
{
    private readonly SubsceneConfig _subsceneConfig;
    private readonly CacheService _cacheService;

    public SubsceneService(
        IOptionsSnapshot<SubsceneConfig> subsceneConfig,
        CacheService cacheService
    )
    {
        _subsceneConfig = subsceneConfig.Value;
        _cacheService = cacheService;
    }

    public async Task<List<IHtmlAnchorElement>> SearchByTitle(string title)
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

        return htmlAnchorElements;
    }

    public async Task<List<IHtmlAnchorElement>> SearchBySlug(string slug)
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
        var address = $"https://sub.pirated.my.id/subtitles/{slug}";
        var document = await AnglesharpUtil.DefaultContext.OpenAsync(address);
        var querySelectorAll = document.QuerySelectorAll<IHtmlAnchorElement>("a[href ^= '/subtitles']");
        var subtitleSelector = querySelectorAll.FirstOrDefault(element => element.Href.Contains("text"));
        var subtitleUrl = subtitleSelector!.Href;

        var localPath = "subtitles/" + slug;
        var fileName = slug.Replace("/", "_") + ".zip";

        var filePath = await subtitleUrl.MultiThreadDownloadFileAsync(localPath, fileName: fileName);

        return filePath;
    }
}
