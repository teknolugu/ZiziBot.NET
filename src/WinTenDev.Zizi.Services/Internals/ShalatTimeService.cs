using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using RepoDb;
using WinTenDev.Zizi.Models.Tables;

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

        public async Task SaveCityAsync(ShalatTime shalatTime)
        {
            var isExist = await DbConnection.ExistsAsync<ShalatTime>(
                time =>
                    time.ChatId == shalatTime.ChatId && time.UserId == shalatTime.UserId
            );

            if (isExist)
            {
                _logger.LogDebug(
                    "Update ShalatTime data. ChatId: {ChatId}, UserId: {UserId}",
                    shalatTime.ChatId,
                    shalatTime.UserId
                );

                var update = await DbConnection.UpdateAsync(
                    shalatTime,
                    fields: Field.Parse<ShalatTime>(
                        column => new
                        {
                            column.UserId,
                            column.ChatId,
                            column.CityId,
                            column.CityName,
                            column.UpdatedAt
                        }
                    ),
                    where: time =>
                        time.ChatId == shalatTime.ChatId &&
                        time.UserId == shalatTime.UserId
                );
            }
            else
            {
                _logger.LogDebug(
                    "Insert ShalatTime data. ChatId: {ChatId}, UserId: {UserId}",
                    shalatTime.ChatId,
                    shalatTime.UserId
                );
                var insert = await DbConnection.InsertAsync(shalatTime);
            }

            _logger.LogInformation(
                "ShalatTime data saved. ChatId: {ChatId}, UserId: {UserId}",
                shalatTime.ChatId,
                shalatTime.UserId
            );
        }

        public async Task<ShalatTime> GetCityByChatId(long chatId)
        {
            var shalatTime = await DbConnection.QueryAsync<ShalatTime>(time => time.ChatId == chatId);

            return shalatTime.FirstOrDefault();
        }
    }
}
