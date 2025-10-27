using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using WeatherApi.Services;
using Xunit;

public class UserPreferenceServiceTests
{
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<UserPreferenceService>> _mockLogger;
    private readonly UserPreferenceService _service;

    public UserPreferenceServiceTests()
    {
        // Use real in-memory cache for functional behavior
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<UserPreferenceService>>();
        _service = new UserPreferenceService(_cache, _mockLogger.Object);
    }

    [Fact]
    public async Task SetDefaultLocationAsync_StoresCityInCache()
    {
        // Arrange
        string userId = "user1";
        string city = "Paris";

        // Act
        await _service.SetDefaultLocationAsync(userId, city);

        // Assert
        _cache.TryGetValue(userId, out string? storedCity);
        Assert.Equal(city, storedCity);
    }

    [Fact]
    public async Task SetDefaultLocationAsync_TrimCityBeforeStoring()
    {
        // Arrange
        string userId = "user2";
        string city = "  London  ";

        // Act
        await _service.SetDefaultLocationAsync(userId, city);

        // Assert
        _cache.TryGetValue(userId, out string? storedCity);
        Assert.Equal("London", storedCity);
    }

    [Fact]
    public async Task GetDefaultLocationAsync_ReturnsStoredCity()
    {
        // Arrange
        string userId = "user3";
        _cache.Set(userId, "Berlin");

        // Act
        var city = await _service.GetDefaultLocationAsync(userId);

        // Assert
        Assert.Equal("Berlin", city);
    }

    [Fact]
    public async Task GetDefaultLocationAsync_ReturnsNullIfNotFound()
    {
        // Act
        var result = await _service.GetDefaultLocationAsync("unknown");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetDefaultLocationAsync_LogsInformation()
    {
        // Arrange
        string userId = "user4";
        string city = "Rome";

        // Act
        await _service.SetDefaultLocationAsync(userId, city);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Default location set")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
