using System;
using System.Threading.Tasks;
using CacheTower;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using WinTenDev.Zizi.Models.Vendors.FathimahApi;

namespace WinTenDev.Zizi.Services.Externals
{
    public class FathimahApiService
    {
        private const string BaseUrl = "https://api.banghasan.com";
        private readonly ILogger<FathimahApiService> _logger;
        private readonly CacheStack _cacheStack;

        public FathimahApiService(
            ILogger<FathimahApiService> logger,
            CacheStack cacheStack
        )
        {
            _logger = logger;
            _cacheStack = cacheStack;
        }

        public async Task<CityResponse> GetAllCityAsync()
        {
            var apis = await BaseUrl
                .AppendPathSegment("sholat/format/json/kota")
                .GetJsonAsync<CityResponse>();

            return apis;
        }

        public async Task<ShalatTimeResponse> GetShalatTime(
            DateTime dateTime,
            long cityId
        )
        {
            var dateStr = dateTime.ToString("yyyy-MM-dd");
            var cacheKey = $"{BaseUrl}_shalat-time_{cityId}_{dateStr}";

            _logger.LogInformation(
                "Get Shalat time for ChatId: {CityId} with Date: {DateStr}. Cache Key: {CacheKey}",
                cityId,
                dateStr,
                cacheKey
            );

            var apis = await _cacheStack.GetOrSetAsync<ShalatTimeResponse>(
                cacheKey: cacheKey,
                async (_) => {
                    var apis = await BaseUrl
                        .AppendPathSegment($"sholat/format/json/jadwal/kota/{cityId}/tanggal/{dateStr}")
                        .GetJsonAsync<ShalatTimeResponse>();

                    return apis;
                },
                new CacheSettings(TimeSpan.FromDays(1))
            );

            return apis;
        }
    }
}
