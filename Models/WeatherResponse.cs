namespace WeatherApp.Models;

public class WeatherResponse
{
    public string Name { get; set; } = string.Empty;
    public MainInfo Main { get; set; } = new();
    public List<WeatherInfo> Weather { get; set; } = new();
    public WindInfo Wind { get; set; } = new();
    public SysInfo Sys { get; set; } = new();
    public int Timezone { get; set; }
}

public class MainInfo
{
    public double Temp { get; set; }
    public double Feels_Like { get; set; }
    public int Humidity { get; set; }
}

public class WeatherInfo
{
    public string Main { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}
public class WindInfo
{
    public double Speed { get; set; }
}

public class SysInfo
{
    public long Sunrise { get; set; }
    public long Sunset { get; set; }
}
// Raw API response classes for the Forecast endpoint
public class ForecastResponse
{
    public List<ForecastItem> List { get; set; } = new();
}

public class ForecastItem
{
    public long Dt { get; set; }
    public MainInfo Main { get; set; } = new();
    public List<WeatherInfo> Weather { get; set; } = new();
}