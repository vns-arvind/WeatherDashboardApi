// -----------------------------------------------------------------------------------------------------
//  Summary:
//      This service provides an in-memory implementation of user preference management,
//      focusing on storing and retrieving a user's default weather location. It uses
//      IMemoryCache for fast, transient data storage and ILogger for operational logging.
//
//  Responsibilities:
//      - Store a user's default city selection in memory.
//      - Retrieve the default location for a given user ID.
//      - Automatically expire cached preferences after a configurable sliding duration.
//
//  Dependencies:
//      - IMemoryCache: Manages temporary storage of user preferences in memory.
//      - ILogger<UserPreferenceService>: Logs operations such as saving or retrieving preferences.
//
//  Caching Policy:
//      - Each user's default location is cached with a sliding expiration of 6 hours.
//      - Cache entries reset their lifetime upon access to maintain active users' data.
//
//  Error Handling:
//      - Minimal, as this service operates entirely in-memory and performs simple operations.
// -----------------------------------------------------------------------------------------------------
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
