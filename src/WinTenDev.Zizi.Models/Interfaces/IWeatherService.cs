using System.Threading.Tasks;
using WinTenDev.Zizi.Models.Types;

namespace WinTenDev.Zizi.Models.Interfaces;

public interface IWeatherService
{
    Task<CurrentWeather> GetWeatherAsync(double lat, double lon);
}