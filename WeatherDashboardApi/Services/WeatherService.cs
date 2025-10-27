using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using WeatherApi.Models;

namespace WeatherApi.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<WeatherService> _logger;
        private readonly string _apiKey;

        public WeatherService(
            IHttpClientFactory httpFactory,
            IMemoryCache cache,
            ILogger<WeatherService> logger,
            IOptions<OpenWeatherMapSettings> settings)
        {
            _httpFactory = httpFactory;
            _cache = cache;
            _logger = logger;
            _apiKey = settings.Value.ApiKey;
        }

        public async Task<Weather> GetWeatherByCityAsync(string city)
        {
            // City name validation is now handled by FluentValidation — keep only sanity check
            city = city.Trim();
            string cacheKey = GetCacheKey(city);

            if (TryGetCachedWeather(cacheKey, out var cached))
                return cached!;

            // If API key missing, return fallback data
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("No API key configured. Returning sample data for {city}", city);
                return CacheWeather(cacheKey, GetSampleWeather(city));
            }

            // Try fetching from external provider
            var json = await FetchWeatherFromApiAsync(city);
            var result = await ParseWeatherFromJsonAsync(json, city);

            return CacheWeather(cacheKey, result);
        }

        private static string GetCacheKey(string city) => $"weather_{city.ToLowerInvariant()}";

        private bool TryGetCachedWeather(string cacheKey, out Weather? weather)
        {
            if (_cache.TryGetValue(cacheKey, out weather))
            {
                _logger.LogInformation("Cache hit for {city}", cacheKey);
                return true;
            }
            return false;
        }

        private Weather GetSampleWeather(string city)
        {
            return new Weather(city, "XX", 20.0, "Clear sky (sample)", 50, 3.5);
        }

        private async Task<string> FetchWeatherFromApiAsync(string city)
        {
            var client = _httpFactory.CreateClient("OpenWeather");
            var url = $"weather?q={WebUtility.UrlEncode(city)}&appid={_apiKey}&units=metric";

            try
            {
                var resp = await client.GetAsync(url);
                var body = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenWeatherMap returned {status}: {body}", resp.StatusCode, body);
                    throw new InvalidOperationException($"Weather provider error: {body} errorCode: {resp.StatusCode}");
                }

                return body;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to reach OpenWeather API for city {city}", city);
                throw new InvalidOperationException("Unable to reach weather provider. Please try again later.", ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Weather API call timed out for city {city}", city);
                throw new InvalidOperationException("Weather service timed out. Please try again later.", ex);
            }
        }

        public async Task<Weather> ParseWeatherFromJsonAsync(string json, string city)
        {
            try
            {
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
                using var doc = await JsonDocument.ParseAsync(stream);
                var root = doc.RootElement;

                var cityName = root.GetProperty("name").GetString() ?? city;
                var country = root.GetProperty("sys").GetProperty("country").GetString() ?? "";
                var temp = root.GetProperty("main").GetProperty("temp").GetDouble();
                var desc = root.GetProperty("weather")[0].GetProperty("description").GetString() ?? "";
                var humidity = root.GetProperty("main").GetProperty("humidity").GetInt32();
                var wind = root.GetProperty("wind").GetProperty("speed").GetDouble();

                return new Weather(cityName, country, temp, desc, humidity, wind);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse weather data for {city}", city);
                throw new InvalidOperationException("Weather provider returned malformed data.", ex);
            }
        }

        private Weather CacheWeather(string cacheKey, Weather weather)
        {
            _cache.Set(cacheKey, weather, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
            return weather;
        }
    }
}
