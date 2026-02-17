using WeatherApp.Models;

namespace WeatherApp.Services
{
    public interface IWeatherService
    {
        Task<WeatherModel?> GetWeatherAsync(string city, string units = "metric");
        Task<List<ForecastModel>> GetForecastAsync(string city, string units = "metric");
        Task<List<CityResponse>> SearchCitiesAsync(string query);
        Task<WeatherModel?> GetWeatherByLocationAsync(double lat, double lon, string units = "metric");
        Task<List<ForecastModel>> GetForecastByLocationAsync(double lat, double lon, string units = "metric");

    }
}
