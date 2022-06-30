using System;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using WinTenDev.Zizi.Models.Vendors.FathimahApi;
using WinTenDev.Zizi.Services.Internals;

namespace WinTenDev.Zizi.Services.Externals
{
    public class FathimahApiService
    {
        private const string BaseUrl = "https://api.banghasan.com";
        private readonly ILogger<FathimahApiService> _logger;
        private readonly CacheService _cacheService;

        public FathimahApiService(
            ILogger<FathimahApiService> logger,
            CacheService cacheService
        )
        {
            _logger = logger;
            _cacheService = cacheService;
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

            var apis = await _cacheService.GetOrSetAsync(
                cacheKey: cacheKey,
                expireAfter: "1d",
                staleAfter: "1h",
                action: async () => {
                    var apis = await BaseUrl
                        .AppendPathSegment($"sholat/format/json/jadwal/kota/{cityId}/tanggal/{dateStr}")
                        .GetJsonAsync<ShalatTimeResponse>();

                    return apis;
                }
            );

            return apis;
        }
    }
}