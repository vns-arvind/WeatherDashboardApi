namespace WeatherApi.Services
{
    public interface IUserPreferenceService
    {
        Task SetDefaultLocationAsync(string userId, string city);
        Task<string?> GetDefaultLocationAsync(string userId);
    }
}
