using System.Text.Json;

namespace Weather.Frontend;

public class WeatherService
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _client;

    public WeatherService(HttpClient client)
    {
        _client = client;
    }

    public async Task<WeatherForecast[]> GetWeatherForecastAsync()
    {
        return await _client.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast");
    }
}
