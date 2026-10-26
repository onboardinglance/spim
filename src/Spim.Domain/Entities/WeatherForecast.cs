using Spim.Domain.Common;
using Spim.Domain.Exceptions;

namespace Spim.Domain.Entities;

public sealed class WeatherForecast : AuditableEntity
{
    public const int MinTemperatureC = -90;
    public const int MaxTemperatureC = 60;
    public const int SummaryMaxLength = 100;

    // Required by EF Core for materialisation.
    private WeatherForecast() { }

    public DateOnly Date { get; private set; }

    public int TemperatureC { get; private set; }

    public string? Summary { get; private set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public static WeatherForecast Create(DateOnly date, int temperatureC, string? summary)
    {
        var forecast = new WeatherForecast();
        forecast.Update(date, temperatureC, summary);
        return forecast;
    }

    public void Update(DateOnly date, int temperatureC, string? summary)
    {
        if (temperatureC is < MinTemperatureC or > MaxTemperatureC)
            throw new DomainException($"Temperature must be between {MinTemperatureC}°C and {MaxTemperatureC}°C.");

        summary = summary?.Trim();
        if (summary?.Length > SummaryMaxLength)
            throw new DomainException($"Summary must not exceed {SummaryMaxLength} characters.");

        Date = date;
        TemperatureC = temperatureC;
        Summary = string.IsNullOrEmpty(summary) ? DescribeTemperature(temperatureC) : summary;
    }

    public static string DescribeTemperature(int temperatureC) => temperatureC switch
    {
        < -10 => "Freezing",
        < 0 => "Bracing",
        < 5 => "Chilly",
        < 12 => "Cool",
        < 18 => "Mild",
        < 24 => "Warm",
        < 28 => "Balmy",
        < 33 => "Hot",
        < 40 => "Sweltering",
        _ => "Scorching"
    };
}
