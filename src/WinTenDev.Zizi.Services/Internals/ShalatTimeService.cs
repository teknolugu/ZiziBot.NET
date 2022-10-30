using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using MongoDB.Entities;
using MySqlConnector;
using RepoDb;

namespace WinTenDev.Zizi.Services.Internals
{
    public class ShalatTimeService
    {
        private readonly ILogger<ShalatTimeService> _logger;
        private readonly IMapper _mapper;
        private readonly QueryService _queryService;

        public ShalatTimeService(
            ILogger<ShalatTimeService> logger,
            IMapper mapper,
            QueryService queryService
        )
        {
            _logger = logger;
            _mapper = mapper;
            _queryService = queryService;
        }

        public MySqlConnection DbConnection => _queryService.CreateMysqlConnectionCore();

        public async Task<bool> IsExistAsync(
            long chatId,
            string cityName
        )
        {
            // var isExist = await DbConnection.ExistsAsync<ShalatTime>(
            //     time =>
            //         time.ChatId == chatId && time.CityName == cityName
            // );

            var isExist = await DB.Find<CityEntity>()
                .Match(entity =>
                    entity.ChatId == chatId &&
                    string.Equals(entity.CityName, cityName, StringComparison.OrdinalIgnoreCase)
                )
                .ExecuteAnyAsync();

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

        public async Task SaveCityAsync(CityDto city)
        {
            _logger.LogDebug(
                "Insert ShalatTime data. ChatId: {ChatId}, UserId: {UserId}",
                city.ChatId,
                city.UserId
            );

            var shalatTimeEntity = _mapper.Map<CityEntity>(city);
            await DB.InsertAsync(shalatTimeEntity);

            _logger.LogInformation(
                "ShalatTime data saved. ChatId: {ChatId}, UserId: {UserId}",
                city.ChatId,
                city.UserId
            );
        }

        public async Task<List<CityEntity>> GetCities()
        {
            // var shalatTime = await DbConnection.QueryAllAsync<ShalatTime>();
            var cities = await DB.Find<CityEntity>()
                .ExecuteAsync();

            return cities;
        }

        public async Task<List<CityEntity>> GetCities(long chatId)
        {
            var cities = await GetCities();

            return cities
                .Where(entity => entity.ChatId == chatId)
                .OrderBy(entity => entity.CityName)
                .ToList();
        }

        public async Task<ShalatTime> GetCityByChatId(long chatId)
        {
            var shalatTime = await DbConnection
                .QueryAsync<ShalatTime>(time => time.ChatId == chatId);

            return shalatTime.FirstOrDefault();
        }

        public async Task<long> DeleteCityAsync(
            long chatId,
            string cityName
        )
        {
            // var delete = await DbConnection.DeleteAsync<ShalatTime>(
            //     time =>
            //         time.ChatId == chatId && time.CityName == cityName
            // );

            var deleteResult = await DB.DeleteAsync<CityEntity>(entity =>
                entity.ChatId == chatId &&
                entity.CityName.ToLower() == cityName
            );

            return deleteResult.DeletedCount;
        }
    }
}