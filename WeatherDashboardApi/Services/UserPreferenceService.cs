using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WeatherApi.Services;

public class UserPreferenceService : IUserPreferenceService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserPreferenceService> _logger;

    public UserPreferenceService(IMemoryCache cache, ILogger<UserPreferenceService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task SetDefaultLocationAsync(string userId, string city)
    {
        _cache.Set(userId, city.Trim(), new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(6)
        });

        _logger.LogInformation("Default location set for {userId}: {city}", userId, city);
        return Task.CompletedTask;
    }

    public Task<string?> GetDefaultLocationAsync(string userId)
    {
        _cache.TryGetValue(userId, out string? city);
        return Task.FromResult(city);
    }
}
