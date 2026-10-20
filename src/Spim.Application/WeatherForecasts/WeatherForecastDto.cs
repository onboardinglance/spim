namespace Spim.Application.WeatherForecasts;

public sealed class WeatherForecastDto
{
    public int Id { get; init; }

    public DateOnly Date { get; init; }

    public int TemperatureC { get; init; }

    public int TemperatureF { get; init; }

    public string? Summary { get; init; }
}
