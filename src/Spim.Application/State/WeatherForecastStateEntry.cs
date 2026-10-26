using Spim.Domain.Entities;

namespace Spim.Application.State;

public sealed record WeatherForecastStateEntry(string Key, WeatherForecast? Value, string? ETag);

public sealed record StateWrite<T>(string Key, T Value);