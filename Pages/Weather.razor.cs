using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using WeatherApp.Models;
using WeatherApp.Services;

namespace WeatherApp.Pages; // Ensure this matches your folder structure

public partial class Weather : ComponentBase
{
    [Inject] public IWeatherService WeatherService { get; set; } = default!;
    [Inject] public IJSRuntime JS { get; set; } = default!;

    private string city = string.Empty;
    private WeatherModel? weather;
    private bool loading;
    private bool searched;
    private bool darkMode;

    private List<CityResponse> cityResults = new();
    private List<string> lastSearched = new();
    private int selectedIndex = -1;
    private CancellationTokenSource? cts;
    private List<ForecastModel> forecasts = new();
    private bool isCelsius = true;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var savedDarkMode = await JS.InvokeAsync<string>("localStorage.getItem", "darkMode");
            darkMode = savedDarkMode == "true";

            var savedCities = await JS.InvokeAsync<string>("localStorage.getItem", "lastCities");
            if (!string.IsNullOrWhiteSpace(savedCities))
                lastSearched = savedCities.Split('|').ToList();

            var savedUnit = await JS.InvokeAsync<string>("localStorage.getItem", "useCelsius");
            if (!string.IsNullOrWhiteSpace(savedUnit))
                isCelsius = savedUnit == "true";

            // FIX 1: Thread-safe UI update
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task SetUnit(bool toCelsius)
    {
        if (isCelsius == toCelsius) return;

        isCelsius = toCelsius;
        await JS.InvokeVoidAsync("localStorage.setItem", "useCelsius", isCelsius.ToString().ToLower());

        if (!string.IsNullOrWhiteSpace(city) && weather != null)
        {
            await GetWeather();
        }
    }

    private void ToggleDarkMode()
    {
        darkMode = !darkMode;
        JS.InvokeVoidAsync("localStorage.setItem", "darkMode", darkMode.ToString().ToLower());
    }

    private async Task OnCityInput(ChangeEventArgs e)
    {
        city = e.Value?.ToString() ?? string.Empty;

        cts?.Cancel();
        cts = new CancellationTokenSource();
        var token = cts.Token;

        if (city.Length < 2)
        {
            cityResults.Clear();
            await InvokeAsync(StateHasChanged); // FIX 1
            return;
        }

        try
        {
            await Task.Delay(300, token);
            if (!token.IsCancellationRequested)
            {
                cityResults = await WeatherService.SearchCitiesAsync(city);
                await InvokeAsync(StateHasChanged); // FIX 1
            }
        }
        catch (TaskCanceledException) { }
    }

    private void OnKeyDown(KeyboardEventArgs e)
    {
        if (!cityResults.Any()) return;

        if (e.Key == "ArrowDown")
            selectedIndex = (selectedIndex + 1) % cityResults.Count;
        else if (e.Key == "ArrowUp")
            selectedIndex = (selectedIndex - 1 + cityResults.Count) % cityResults.Count;
        else if (e.Key == "Enter")
        {
            if (selectedIndex >= 0 && selectedIndex < cityResults.Count)
            {
                var selectedItem = cityResults[selectedIndex];
                SelectCity($"{selectedItem.Name}, {selectedItem.Country}");
                selectedIndex = -1;
            }
        }
    }

    private async Task ClearHistory()
    {
        lastSearched.Clear();
        await JS.InvokeVoidAsync("localStorage.removeItem", "lastCities");
    }

    private void SelectCity(string selectedCity)
    {
        city = selectedCity;
        cityResults.Clear();
    }

    private async Task SearchFromHistory(string historicCity)
    {
        city = historicCity;
        await GetWeather();
    }

    private async Task GetWeather()
    {
        if (string.IsNullOrWhiteSpace(city)) return;

        loading = true;
        searched = true;
        cityResults.Clear();
        weather = null;
        forecasts.Clear();

        string unitParam = isCelsius ? "metric" : "imperial";
        weather = await WeatherService.GetWeatherAsync(city, unitParam);

        if (weather != null)
        {
            forecasts = await WeatherService.GetForecastAsync(city);

            if (!lastSearched.Contains(city))
            {
                lastSearched.Insert(0, city);
                if (lastSearched.Count > 5) lastSearched.RemoveAt(5);

                // FIX 2: Reused the SaveHistory method to prevent duplication
                await SaveHistory();
            }
        }

        loading = false;
        await InvokeAsync(StateHasChanged); // FIX 1
    }

    private async Task GetLocationWeather()
    {
        loading = true;
        try
        {
            var loc = await JS.InvokeAsync<LocationModel>("window.getUserLocation");
            string unitParam = isCelsius ? "metric" : "imperial";

            weather = await WeatherService.GetWeatherByLocationAsync(loc.Latitude, loc.Longitude, unitParam);

            if (weather != null)
            {
                forecasts = await WeatherService.GetForecastByLocationAsync(loc.Latitude, loc.Longitude, unitParam);

                city = $"{weather.City}, GPS";
                searched = true;

                if (!lastSearched.Contains(city))
                {
                    lastSearched.Insert(0, city);
                    if (lastSearched.Count > 5)
                    {
                        lastSearched.RemoveAt(lastSearched.Count - 1);
                    }
                    await SaveHistory();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting location: {ex.Message}");
        }
        finally
        {
            loading = false;
            await InvokeAsync(StateHasChanged); // FIX 1
        }
    }

    private async Task SaveHistory()
    {
        // FIX 3: Unified Local Storage saving. 
        // Changed to use "lastCities" and a pipe delimiter so OnAfterRenderAsync can read it correctly.
        await JS.InvokeVoidAsync("localStorage.setItem", "lastCities", string.Join("|", lastSearched));
    }
}