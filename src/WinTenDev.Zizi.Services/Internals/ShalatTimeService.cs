using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using RepoDb;

namespace WinTenDev.Zizi.Services.Internals
{
    public class ShalatTimeService
    {
        private readonly ILogger<ShalatTimeService> _logger;
        private readonly QueryService _queryService;

        public ShalatTimeService(
            ILogger<ShalatTimeService> logger,
            QueryService queryService
        )
        {
            _logger = logger;
            _queryService = queryService;
        }

        public MySqlConnection DbConnection => _queryService.CreateMysqlConnectionCore();

        public async Task<bool> IsExistAsync(
            long chatId,
            string cityName
        )
        {
            var isExist = await DbConnection.ExistsAsync<ShalatTime>(
                time =>
                    time.ChatId == chatId && time.CityName == cityName
            );

            return isExist;
        }

        public async Task SaveCityAsync(ShalatTime shalatTime)
        {
            _logger.LogDebug(
                "Insert ShalatTime data. ChatId: {ChatId}, UserId: {UserId}",
                shalatTime.ChatId,
                shalatTime.UserId
            );
            var insert = await DbConnection.InsertAsync(shalatTime);

            _logger.LogInformation(
                "ShalatTime data saved. ChatId: {ChatId}, UserId: {UserId}. Result: {Insert}",
                shalatTime.ChatId,
                shalatTime.UserId,
                insert
            );
        }

        public async Task<IEnumerable<ShalatTime>> GetCities()
        {
            var shalatTime = await DbConnection.QueryAllAsync<ShalatTime>();

            return shalatTime;
        }

        public async Task<List<ShalatTime>> GetCities(long chatId)
        {
            var shalatTimes = await GetCities();

            return shalatTimes
                .Where(shalatTime => shalatTime.ChatId == chatId)
                .OrderBy(shalatTime => shalatTime.CityName)
                .ToList();
        }

        public async Task<ShalatTime> GetCityByChatId(long chatId)
        {
            var shalatTime = await DbConnection
                .QueryAsync<ShalatTime>(time => time.ChatId == chatId);

            return shalatTime.FirstOrDefault();
        }

        public async Task<int> DeleteCityAsync(
            long chatId,
            string cityName
        )
        {
            var delete = await DbConnection.DeleteAsync<ShalatTime>(
                time =>
                    time.ChatId == chatId && time.CityName == cityName
            );

            return delete;
        }
    }
}