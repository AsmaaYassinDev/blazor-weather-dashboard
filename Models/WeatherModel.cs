namespace WeatherApp.Models;

public class WeatherModel
{
    public string City { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double FeelsLike { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Humidity { get; set; }
    public string Icon { get; set; } = string.Empty;
    public double WindSpeed { get; set; }
    public string Sunrise { get; set; } = string.Empty;
    public string Sunset { get; set; } = string.Empty;
}
public class ForecastModel
{
    public DateTime Date { get; set; }
    public double Temp { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
public class LocationModel
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}