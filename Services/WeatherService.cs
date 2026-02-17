using WeatherApp.Models;

namespace WeatherApp.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(HttpClient http, IConfiguration config, ILogger<WeatherService> logger)
    {
        _http = http;
        // Fallback to empty string to avoid null exceptions if secret is missing
        _apiKey = config["OpenWeatherMap:ApiKey"] ?? string.Empty;
        _logger = logger;
    }

    public async Task<WeatherModel?> GetWeatherAsync(string city,string units)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("OpenWeatherMap API Key is missing.");
            return null;
        }

        try
        {
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={_apiKey}&units={units}";
            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch weather for {City}. Status: {StatusCode}", city, response.StatusCode);
                return null;
            }

            var apiData = await response.Content.ReadFromJsonAsync<WeatherResponse>();

            if (apiData?.Weather == null || !apiData.Weather.Any())
            {
                _logger.LogWarning("Weather data format was invalid or empty for {City}.", city);
                return null;
            }
            // Convert Unix timestamps to the searched city's actual local time
            var timeOffset = TimeSpan.FromSeconds(apiData.Timezone);
            var sunriseTime = DateTimeOffset.FromUnixTimeSeconds(apiData.Sys.Sunrise).ToOffset(timeOffset);
            var sunsetTime = DateTimeOffset.FromUnixTimeSeconds(apiData.Sys.Sunset).ToOffset(timeOffset);
            return new WeatherModel
            {
                City = apiData.Name,
                Temperature = apiData.Main.Temp,
                FeelsLike = apiData.Main.Feels_Like,
                Humidity = apiData.Main.Humidity,
                Description = apiData.Weather.First().Description,
                Icon = apiData.Weather.First().Icon,
                // Map the new properties:
                WindSpeed = apiData.Wind.Speed,
                Sunrise = sunriseTime.ToString("h:mm tt"), // Formats as "6:45 AM"
                Sunset = sunsetTime.ToString("h:mm tt")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching weather for {City}.", city);
            return null;
        }
    }

    public async Task<List<CityResponse>> SearchCitiesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2 || string.IsNullOrWhiteSpace(_apiKey))
            return new List<CityResponse>();

        try
        {
            var url = $"https://api.openweathermap.org/geo/1.0/direct?q={query}&limit=5&appid={_apiKey}";
            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to search cities for '{Query}'. Status: {StatusCode}", query, response.StatusCode);
                return new List<CityResponse>();
            }

            var cities = await response.Content.ReadFromJsonAsync<List<CityResponse>>();
            return cities ?? new List<CityResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while searching for cities with query '{Query}'.", query);
            return new List<CityResponse>();
        }
    }
    public async Task<List<ForecastModel>> GetForecastAsync(string city, string units)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return new List<ForecastModel>();

        try
        {
            var url = $"https://api.openweathermap.org/data/2.5/forecast?q={city}&appid={_apiKey}&units={units}";
            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch forecast for {City}.", city);
                return new List<ForecastModel>();
            }

            var apiData = await response.Content.ReadFromJsonAsync<ForecastResponse>();
            if (apiData?.List == null) return new List<ForecastModel>();

            // Group by date to get 5 distinct days from the 40 3-hour blocks
            var dailyForecasts = apiData.List
                .Select(f => new
                {
                    DateTime = DateTimeOffset.FromUnixTimeSeconds(f.Dt).UtcDateTime,
                    Item = f
                })
                .GroupBy(x => x.DateTime.Date)
                .Where(g => g.Key > DateTime.UtcNow.Date) // Only take future days
                .Take(5)
                .Select(g =>
                {
                    // Pick the reading closest to midday to represent the day
                    var bestReading = g.FirstOrDefault(x => x.DateTime.Hour >= 11 && x.DateTime.Hour <= 15) ?? g.First();
                    return new ForecastModel
                    {
                        Date = bestReading.DateTime,
                        Temp = bestReading.Item.Main.Temp,
                        Icon = bestReading.Item.Weather.First().Icon,
                        Description = bestReading.Item.Weather.First().Description
                    };
                })
                .ToList();

            return dailyForecasts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching forecast for {City}", city);
            return new List<ForecastModel>();
        }
    }
    public async Task<WeatherModel?> GetWeatherByLocationAsync(double lat, double lon, string units = "metric")
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return null;

        try
        {
            var url = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={_apiKey}&units={units}";
            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode) return null;

            var apiData = await response.Content.ReadFromJsonAsync<WeatherResponse>();
            if (apiData?.Weather == null || !apiData.Weather.Any()) return null;

            var timeOffset = TimeSpan.FromSeconds(apiData.Timezone);
            var sunriseTime = DateTimeOffset.FromUnixTimeSeconds(apiData.Sys.Sunrise).ToOffset(timeOffset);
            var sunsetTime = DateTimeOffset.FromUnixTimeSeconds(apiData.Sys.Sunset).ToOffset(timeOffset);

            return new WeatherModel
            {
                City = apiData.Name,
                Temperature = apiData.Main.Temp,
                FeelsLike = apiData.Main.Feels_Like,
                Humidity = apiData.Main.Humidity,
                Description = apiData.Weather.First().Description,
                Icon = apiData.Weather.First().Icon,
                WindSpeed = apiData.Wind.Speed,
                Sunrise = sunriseTime.ToString("h:mm tt"),
                Sunset = sunsetTime.ToString("h:mm tt")
            };
        }
        catch { return null; }
    }

    public async Task<List<ForecastModel>> GetForecastByLocationAsync(double lat, double lon, string units = "metric")
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return new List<ForecastModel>();

        try
        {
            var url = $"https://api.openweathermap.org/data/2.5/forecast?lat={lat}&lon={lon}&appid={_apiKey}&units={units}";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new List<ForecastModel>();

            var apiData = await response.Content.ReadFromJsonAsync<ForecastResponse>();
            if (apiData?.List == null) return new List<ForecastModel>();

            return apiData.List
                .Select(f => new { DateTime = DateTimeOffset.FromUnixTimeSeconds(f.Dt).UtcDateTime, Item = f })
                .GroupBy(x => x.DateTime.Date)
                .Where(g => g.Key > DateTime.UtcNow.Date)
                .Take(5)
                .Select(g =>
                {
                    var bestReading = g.FirstOrDefault(x => x.DateTime.Hour >= 11 && x.DateTime.Hour <= 15) ?? g.First();
                    return new ForecastModel
                    {
                        Date = bestReading.DateTime,
                        Temp = bestReading.Item.Main.Temp,
                        Icon = bestReading.Item.Weather.First().Icon,
                        Description = bestReading.Item.Weather.First().Description
                    };
                }).ToList();
        }
        catch { return new List<ForecastModel>(); }
    }
}