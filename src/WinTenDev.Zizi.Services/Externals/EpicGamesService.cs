using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Hangfire;
using Humanizer;
using MoreLinq;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Externals;

public class EpicGamesService
{
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly ITelegramBotClient _botClient;
    private readonly CacheService _cacheService;
    private readonly RssService _rssService;
    private readonly FeatureService _featureService;
    private readonly string BaseUrl = "https://store-site-backend-static-ipv4.ak.epicgames.com/freeGamesPromotions";

    public EpicGamesService(
        IRecurringJobManager recurringJobManager,
        ITelegramBotClient botClient,
        CacheService cacheService,
        RssService rssService,
        FeatureService featureService
    )
    {
        _recurringJobManager = recurringJobManager;
        _botClient = botClient;
        _cacheService = cacheService;
        _rssService = rssService;
        _featureService = featureService;
    }

    public async Task RegisterJobEpicGamesBroadcaster()
    {
        var feature = await _featureService.GetFeatureConfig("epic-games");

        var allowAt = feature?.AllowsAt;

        allowAt?.ForEach(
            target => {
                var chatId = target.ToInt64();
                var jobId = "egs-free-" + chatId.ReduceChatId();
                _recurringJobManager.AddOrUpdate(
                    recurringJobId: jobId,
                    methodCall: () => SendEpicGamesBroadcaster(chatId),
                    cronExpression: Cron.Minutely
                );
            }
        );
    }

    [JobDisplayName("EpicGames Broadcaster {0}")]
    public async Task SendEpicGamesBroadcaster(long chatId)
    {
        var games = await GetFreeGamesParsed();
        var freeGames = games.FirstOrDefault();

        if (freeGames == null) return;

        var productUrl = freeGames.ProductUrl;
        var productTitle = freeGames.ProductTitle;

        var chat = await _botClient.GetChatAsync(chatId);
        if (chat.LinkedChatId != null) chatId = chat.LinkedChatId.Value;

        var isHistoryExist = await _rssService.IsHistoryExist(chatId, productUrl);
        if (isHistoryExist)
        {
            Log.Information(
                "Seem EpicGames with Title: '{Title}' already sent to ChatId: {ChannelId}",
                productTitle,
                chatId
            );
        }
        else
        {
            await _botClient.SendPhotoAsync(
                chatId: chatId,
                photo: freeGames.Images.ToString(),
                caption: freeGames.Detail,
                parseMode: ParseMode.Html
            );

            await _rssService.SaveRssHistoryAsync(
                new RssHistory()
                {
                    ChatId = chatId,
                    Title = productTitle,
                    Url = productUrl,
                    PublishDate = DateTime.UtcNow,
                    Author = "EpicGames Free",
                    CreatedAt = DateTime.UtcNow,
                    RssSource = "https://store.epicgames.com"
                }
            );
        }
    }

    public async Task<List<EgsFreeGameParsed>> GetFreeGamesParsed(bool preferEmoji = false)
    {
        var egsFreeGame = await GetFreeGamesRaw();
        var offeredGameList = egsFreeGame.DiscountGames.Select(
                (
                    element,
                    index
                ) => {
                    var captionBuilder = new StringBuilder();
                    var detailBuilder = new StringBuilder();

                    var slug = element.ProductSlug ?? element.UrlSlug;
                    var url = Url.Combine("https://www.epicgames.com/store/en-US/p/", slug);
                    var title = element.Title;
                    var titleLink = element.Title.MkUrl(url);

                    var promotionOffers = element.Promotions.PromotionalOffers?.FirstOrDefault()?.PromotionalOffers.FirstOrDefault();
                    var upcomingPromotionalOffers = element.Promotions.UpcomingPromotionalOffers?.FirstOrDefault()?.PromotionalOffers.FirstOrDefault();
                    var offers = promotionOffers ?? upcomingPromotionalOffers;

                    captionBuilder.AppendLine($"{index + 1}. {titleLink}");

                    // if (offers != null)
                    captionBuilder
                        .Append("<b>Offers date:</b> ")
                        .Append(offers?.StartDate?.LocalDateTime.ToString("yyyy-MM-dd hh:mm tt"))
                        .Append(" to ")
                        .Append(offers?.EndDate?.LocalDateTime.ToString("yyyy-MM-dd hh:mm tt"))
                        .AppendLine();

                    detailBuilder.Append(captionBuilder);

                    element.CustomAttributes
                        .Where(attribute => attribute.Key.Contains("Name"))
                        .ForEach(
                            attribute => {
                                var name = attribute.Key.Titleize().Replace("Name", "").Trim();
                                detailBuilder.AppendLine(name + ": " + attribute.Value);
                            }
                        );

                    detailBuilder.AppendLine()
                        .AppendLine(element.Description)
                        .AppendLine();

                    var egsParsed = new EgsFreeGameParsed()
                    {
                        ProductUrl = url,
                        ProductTitle = title,
                        Text = captionBuilder.ToTrimmedString(),
                        Detail = detailBuilder.ToTrimmedString(),
                        Images = element.KeyImages.FirstOrDefault(keyImage => keyImage.Type == "OfferImageWide")?.Url
                    };

                    return egsParsed;
                }
            )
            .ToList();

        return offeredGameList;
    }

    public async Task<EgsFreeGame> GetFreeGamesRaw()
    {
        var egsFreeGame = await GetFreeGamesRawCore(
            new EgsFreeGamesPromotionsDto()
            {
                Country = "ID",
                Locale = "en-US",
                AllowCountries = "ID"
            }
        );

        var allGames = egsFreeGame.Data.Catalog.SearchStore.Elements;
        var freeGames = allGames
            .Where(element => element.Price.TotalPrice.DiscountPrice == 0)
            .ToList();

        var discountGames = allGames
            .Where(
                element =>
                    element.Promotions != null
            )
            .OrderByDescending(element => element.Promotions.PromotionalOffers?.FirstOrDefault()?.PromotionalOffers.FirstOrDefault()?.StartDate)
            .ToList();

        var freeGame = new EgsFreeGame()
        {
            AllGames = allGames,
            FreeGames = freeGames,
            DiscountGames = discountGames
        };

        return freeGame;
    }

    public async Task<EgsFreeGameRaw> GetFreeGamesRawCore(EgsFreeGamesPromotionsDto promotionsDto)
    {
        var freeGamesObj = await _cacheService.GetOrSetAsync(
            "egs-free-games",
            () => {
                var queryParams = promotionsDto.ToDictionary();

                var freeGamesObj = BaseUrl
                    .SetQueryParams(queryParams)
                    .GetJsonAsync<EgsFreeGameRaw>();

                return freeGamesObj;
            }
        );

        return freeGamesObj;
    }
}
