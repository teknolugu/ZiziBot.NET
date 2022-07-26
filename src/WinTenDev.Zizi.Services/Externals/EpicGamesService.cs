using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Hangfire;
using Humanizer;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace WinTenDev.Zizi.Services.Externals;

public class EpicGamesService
{
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly ITelegramBotClient _botClient;
    private readonly CacheService _cacheService;
    private readonly RssService _rssService;
    private readonly FeatureService _featureService;
    private readonly string BaseUrl = "https://store-site-backend-static-ipv4.ak.epicgames.com/freeGamesPromotions";
    private readonly string ProductDetailUrl = "https://store-content-ipv4.ak.epicgames.com/api/en-US/content/products";

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
                    methodCall: () => RunEpicGamesBroadcaster(chatId),
                    cronExpression: Cron.Minutely
                );
            }
        );
    }

    [JobDisplayName("EGS Free {0}")]
    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task RunEpicGamesBroadcaster(long chatId)
    {
        var games = await GetFreeGamesParsed();

        await games.ForEachAsync(async freeGames => {
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
        });
    }

    public async Task<List<IAlbumInputMedia>> GetFreeGamesOffered()
    {
        var buttonMarkup = new List<InlineKeyboardButton>();
        var offeredGameList = await GetFreeGamesParsed(currentOffered: true);

        var lastOffered = offeredGameList.LastOrDefault();

        var listAlbum = offeredGameList
            .SkipLast(1)
            .Select(
                item =>
                    new InputMediaPhoto(item.Images.ToString())
            )
            .Cast<IAlbumInputMedia>()
            .ToList();

        var listGames = offeredGameList.Select(
            parsed => {
                buttonMarkup.Add(
                    InlineKeyboardButton.WithCallbackData(
                        parsed.ProductTitle,
                        $"egs {parsed.ProductSlug}"
                    )
                );
                return parsed.Detail;
            }
        ).JoinStr("\n\n");

        listAlbum
            .Add(
                new InputMediaPhoto(lastOffered.Images.ToString())
                {
                    Caption = listGames,
                    ParseMode = ParseMode.Html,
                }
            );

        return listAlbum;
    }

    public async Task<List<EgsFreeGameParsed>> GetFreeGamesParsed(
        bool preferEmoji = false,
        bool currentOffered = false
    )
    {
        var egsFreeGame = await GetFreeGamesRaw();
        var offeredGameList = egsFreeGame.DiscountGames
            .Where(element =>
                element.Promotions.PromotionalOffers?.Count != 0 ||
                element.Promotions.UpcomingPromotionalOffers?.Count != 0
            )
            .Select(
                (
                    element,
                    index
                ) => {
                    var captionBuilder = new StringBuilder();
                    var detailBuilder = new StringBuilder();

                    var title = element.Title;
                    var mappingPageSlug = element.CatalogNs.Mappings.FirstOrDefault()?.PageSlug;
                    var slug = element.ProductSlug ?? element.UrlSlug;
                    var productSlug = element.ProductSlug ?? mappingPageSlug;
                    var gameUrl = Url.Combine("https://www.epicgames.com/store/en-US/p/", mappingPageSlug);
                    var titleLink = element.Title.MkUrl(gameUrl);

                    var promotionOffers = element.Promotions.PromotionalOffers?.FirstOrDefault()?.PromotionalOffers.FirstOrDefault();
                    var upcomingPromotionalOffers = element.Promotions.UpcomingPromotionalOffers?.FirstOrDefault()?.PromotionalOffers.FirstOrDefault();
                    var offers = promotionOffers ?? upcomingPromotionalOffers;

                    captionBuilder.Append(index + 1).Append(". ").AppendLine(titleLink);

                    captionBuilder
                        .Append("<b>Offers date:</b> ")
                        .Append(offers?.StartDate?.LocalDateTime.ToString("yyyy-MM-dd hh:mm tt"))
                        .Append(" to ")
                        .Append(offers?.EndDate?.LocalDateTime.ToString("yyyy-MM-dd hh:mm tt"))
                        .AppendLine();

                    detailBuilder.Append(captionBuilder);

                    element.CustomAttributes
                        .Where(attribute => attribute.Key.Contains("Name"))
                        .ToList()
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
                        ProductUrl = gameUrl,
                        ProductSlug = productSlug,
                        ProductTitle = title,
                        Text = captionBuilder.ToTrimmedString(),
                        StartOfferDate = offers.StartDate,
                        EndOfferDate = offers.EndDate,
                        Detail = detailBuilder.ToTrimmedString(),
                        Images = element.KeyImages.FirstOrDefault(keyImage => keyImage.Type == "OfferImageWide")?.Url
                    };

                    return egsParsed;
                }
            );

        if (currentOffered)
        {
            offeredGameList = offeredGameList.Where(
                game => game.StartOfferDate <= DateTime.UtcNow && game.EndOfferDate >= DateTime.UtcNow
            ).ToList();
        }

        return offeredGameList.ToList();
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
            .Where(element => element.Price?.TotalPrice.DiscountPrice == 0)
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
                    .OpenFlurlSession()
                    .SetQueryParams(queryParams)
                    .GetJsonAsync<EgsFreeGameRaw>();

                return freeGamesObj;
            }
        );

        return freeGamesObj;
    }

    public async Task<EgsFreeGamesDetail> GetGameDetail(string slug)
    {
        var obj = await ProductDetailUrl
            .OpenFlurlSession()
            .AppendPathSegment(slug)
            .GetJsonAsync<EgsFreeGamesDetail>();

        return obj;
    }
}