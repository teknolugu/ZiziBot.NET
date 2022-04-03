using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
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
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly ITelegramBotClient _botClient;
        private readonly ShalatTimeService _shalatTimeService;
        private readonly FathimahApiService _fathimahApiService;

        public ShalatTimeNotifyService(
            IRecurringJobManager recurringJobManager,
            ITelegramBotClient botClient,
            ShalatTimeService shalatTimeService,
            FathimahApiService fathimahApiService
        )
        {
            _recurringJobManager = recurringJobManager;
            _botClient = botClient;
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
                methodCall: (service) => service.SendNotifyAsync(chatId)
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
            var shalatTime = await _shalatTimeService.GetCityByChatId(chatId);

            if (shalatTime == null)
            {
                Log.Debug("Maybe City has removed for ChatId: {ChatId}", chatId);
                return;
            }

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
                Log.Debug("Not Shalat Time found current time at ChatId: {ChatId}", chatId);
                return;
            }

            var sendText = HtmlMessage.Empty
                .Bold(findCurrent.Key).Text($" Telah masuk untuk kawasan {shalatTime.CityName} dan sekitarnya.");

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: sendText.ToString(),
                parseMode: ParseMode.Html
            );

            Log.Debug("Send notification Shalat Time to ChatId: {ChatId} finish", chatId);
        }
    }
}
