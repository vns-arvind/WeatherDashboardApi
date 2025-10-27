using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WeatherApi.Models;
using WeatherApi.Services;
using Xunit;

namespace WeatherApi.Tests.Services
{
    public class WeatherServiceTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly IMemoryCache _cache;
        private readonly WeatherService _service;

        public WeatherServiceTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _cache = new MemoryCache(new MemoryCacheOptions());

            var settings = Options.Create(new OpenWeatherMapSettings
            {
                ApiKey = "test-api-key"
            });

            _service = new WeatherService(
                _httpClientFactoryMock.Object,
                _cache,
                NullLogger<WeatherService>.Instance,
                settings
            );
        }

        [Fact]
        public async Task ParseWeatherFromJsonAsync_ShouldParseValidJson()
        {
            // Arrange
            var json = @"{
              ""name"": ""London"",
              ""sys"": { ""country"": ""GB"" },
              ""main"": { ""temp"": 15.5, ""humidity"": 70 },
              ""weather"": [ { ""description"": ""light rain"" } ],
              ""wind"": { ""speed"": 3.2 }
            }";

            // Act
            var result = await _service.ParseWeatherFromJsonAsync(json, "London");

            // Assert
            Assert.Equal("London", result.City);
            Assert.Equal("GB", result.Country);
            Assert.Equal(15.5, result.TemperatureCelsius);
            Assert.Equal("light rain", result.WeatherDescription);
            Assert.Equal(70, result.Humidity);
            Assert.Equal(3.2, result.WindSpeed);
        }

        [Fact]
        public async Task GetWeatherByCityAsync_ShouldThrow_OnBadResponse()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Unauthorized,
                    Content = new StringContent("{\"cod\":401}")
                });

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("https://api.openweathermap.org/data/2.5/")
            };

            _httpClientFactoryMock
                .Setup(x => x.CreateClient("OpenWeather"))
                .Returns(httpClient);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GetWeatherByCityAsync("London"));
        }

        [Fact]
        public async Task GetWeatherByCityAsync_ShouldReturnFromCache_WhenAvailable()
        {
            // Arrange
            var city = "Paris";
            var weather = new Weather(city, "FR", 20, "Clear", 40, 5);
            _cache.Set("weather_paris", weather);

            // Act
            var result = await _service.GetWeatherByCityAsync(city);

            // Assert
            Assert.Same(weather, result); // Should return cached instance
        }

        [Fact]
        public async Task GetWeatherByCityAsync_ShouldReturnSample_WhenNoApiKey()
        {
            // Arrange
            var settings = Options.Create(new OpenWeatherMapSettings { ApiKey = "" });
            var serviceWithoutKey = new WeatherService(
                _httpClientFactoryMock.Object,
                _cache,
                NullLogger<WeatherService>.Instance,
                settings
            );

            // Act
            var result = await serviceWithoutKey.GetWeatherByCityAsync("Berlin");

            // Assert
            Assert.Equal("Berlin", result.City);
            Assert.Equal("XX", result.Country);
            Assert.Contains("sample", result.WeatherDescription);
        }
    }
}
