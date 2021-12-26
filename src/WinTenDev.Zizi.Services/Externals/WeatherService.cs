using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WinTenDev.Zizi.Models.Interfaces;
using WinTenDev.Zizi.Models.Types;

namespace WinTenDev.Zizi.Services.Externals;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _client;

    public WeatherService()
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri("https://www.metaweather.com/api/")
        };
    }

    public async Task<CurrentWeather> GetWeatherAsync(double lat, double lon)
    {
        var location = await FindLocationIdAsync(lat, lon);

        var today = DateTime.Today;

        var json = await _client.GetStringAsync($"location/{location}/{today.Year}/{today.Month}/{today.Day}");

        dynamic arr = JsonConvert.DeserializeObject(json);

        return new CurrentWeather
        {
            Status = arr[0].weather_state_name,
            Temp = arr[0].the_temp,
            MinTemp = arr[0].min_temp,
            MaxTemp = arr[0].max_temp
        };
    }

    private async Task<string> FindLocationIdAsync(double lat, double lon)
    {
        var json = await _client.GetStringAsync($"location/search?lattlong={lat},{lon}");
        dynamic arr = JsonConvert.DeserializeObject(json);

        return arr[0].woeid;
    }
}