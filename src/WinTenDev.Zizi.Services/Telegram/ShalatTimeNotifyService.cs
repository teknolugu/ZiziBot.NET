using System;
using System.Linq;
using System.Threading.Tasks;
using CacheTower;
using Hangfire;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Externals;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Telegram
{
    public class ShalatTimeNotifyService
    {
        private readonly ILogger<ShalatTimeNotifyService> _logger;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly ITelegramBotClient _botClient;
        private readonly CacheStack _cacheStack;
        private readonly ShalatTimeService _shalatTimeService;
        private readonly FathimahApiService _fathimahApiService;

        public ShalatTimeNotifyService(
            ILogger<ShalatTimeNotifyService> logger,
            IRecurringJobManager recurringJobManager,
            ITelegramBotClient botClient,
            CacheStack cacheStack,
            ShalatTimeService shalatTimeService,
            FathimahApiService fathimahApiService
        )
        {
            _logger = logger;
            _recurringJobManager = recurringJobManager;
            _botClient = botClient;
            _cacheStack = cacheStack;
            _shalatTimeService = shalatTimeService;
            _fathimahApiService = fathimahApiService;
        }

        public string GetJobId(long chatId)
        {
            return "shalat-time_" + chatId.ReduceChatId();
        }

        public async Task RegisterJobShalatTimeAsync()
        {
            var listCity = await _shalatTimeService.GetCities();

            listCity.ForEach(
                time => {
                    var chatId = time.ChatId;
                    RegisterJobShalatTime(chatId);
                }
            );
        }

        public void RegisterJobShalatTime(long chatId)
        {
            _recurringJobManager.AddOrUpdate<ShalatTimeNotifyService>(
                recurringJobId: GetJobId(chatId),
                cronExpression: Cron.Minutely,
                methodCall: (service) =>
                    service.SendNotifyAsync(chatId)
            );
        }

        public void UnRegisterJobShalatTime(long chatId)
        {
            _recurringJobManager.RemoveIfExists(
                recurringJobId: GetJobId(chatId)
            );
        }

        [JobDisplayName("Shalat Time: {0}")]
        public async Task SendNotifyAsync(long chatId)
        {
            Log.Information("Starting send Shalat Time notification to ChatId: {ChatId}", chatId);
            var shalatTimes = await _shalatTimeService.GetCities(chatId);
            var shalatTimesCount = shalatTimes.Count;

            if (shalatTimesCount == 0)
            {
                Log.Debug("No City set for ChatId: {ChatId}", chatId);
                return;
            }

            _logger.LogInformation(
                "Found about {Count} cities Shalat Time configuration for ChatId: {ChatId}",
                shalatTimesCount,
                chatId
            );

            await shalatTimes.AsyncParallelForEach(
                maxDegreeOfParallelism: 8,
                body: async shalatTime => {
                    _logger.LogDebug(
                        "Sending notification for City: {City} to ChatId: {ChatId}",
                        shalatTime.CityId,
                        chatId
                    );

                    var shalatTimeResponse = await _fathimahApiService.GetShalatTime(
                        dateTime: DateTime.Now,
                        cityId: shalatTime.CityId
                    );

                    var currentTime = DateTime.Now.ToString("HH:mm");
                    var jadwalStr = shalatTimeResponse.Jadwal.Data.ToDictionary();
                    var findCurrent = jadwalStr
                        .FirstOrDefault(dictObj => dictObj.Value.ToString() == currentTime);

                    if (findCurrent.IsNull())
                    {
                        Log.Debug(
                            "No Shalat Time found current time for CityId: {CityId} at ChatId: {ChatId}",
                            shalatTime.CityId,
                            chatId
                        );
                        return;
                    }

                    var timeName = findCurrent.Key.ToTitleCase();
                    var cityName = shalatTime.CityName.ToTitleCase();

                    var sendText = HtmlMessage.Empty
                        .Text($"Telah masuk waktu <b>{timeName}</b> untuk <b>{cityName}</b> dan sekitarnya.");

                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: sendText.ToString(),
                        parseMode: ParseMode.Html
                    );

                    _logger.LogDebug(
                        "Notification sent for City: {City} to ChatId: {ChatId}",
                        shalatTime.CityId,
                        chatId
                    );
                }
            );

            Log.Information("Send notification Shalat Time to ChatId: {ChatId} finish", chatId);
        }
    }
}
