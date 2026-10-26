namespace Spim.Application.Models;

public sealed record WeatherForecastData(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public sealed record WeatherForecastDataEntry(string Key, WeatherForecastData? Value, string? ETag);