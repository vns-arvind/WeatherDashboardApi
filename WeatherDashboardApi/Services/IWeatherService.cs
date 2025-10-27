using WeatherApi.Models;

namespace WeatherApi.Services
{
    public interface IWeatherService
    {
        Task<Weather> GetWeatherByCityAsync(string city);
    }
}
