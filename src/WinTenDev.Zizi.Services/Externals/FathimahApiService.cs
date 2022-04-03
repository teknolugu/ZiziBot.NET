using System;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using WinTenDev.Zizi.Models.Vendors.FathimahApi;

namespace WinTenDev.Zizi.Services.Externals
{
    public class FathimahApiService
    {
        private const string BaseUrl = "https://api.banghasan.com";

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
            var apis = await BaseUrl
                .AppendPathSegment($"sholat/format/json/jadwal/kota/{cityId}/tanggal/{dateStr}")
                .GetJsonAsync<ShalatTimeResponse>();

            return apis;
        }

    }
}
