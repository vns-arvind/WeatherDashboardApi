namespace WeatherApi.Models
{
    public record Weather(
        string City,
        string Country,
        double TemperatureCelsius,
        string WeatherDescription,
        int Humidity,
        double WindSpeed
    );
}
