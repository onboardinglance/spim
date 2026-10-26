using Spim.Application.State;
using Spim.Domain.Entities;

namespace Spim.Application.Abstractions;

public interface IWeatherForecastStateStore
{
    Task SaveAsync(string key, WeatherForecast forecast);
    Task<WeatherForecast?> GetAsync(string key);
    Task DeleteAsync(string key);
    Task ExecuteTransactionAsync(IReadOnlyList<StateWrite<WeatherForecast>> writes);
    Task<(WeatherForecast? Value, string? ETag)> GetWithETagAsync(string key);
    Task<bool> TrySaveWithETagAsync(string key, WeatherForecast forecast, string? etag);
    Task SaveWithTtlAsync(string key, WeatherForecast forecast, int ttlSeconds);
    Task SaveWithStrongConsistencyAsync(string key, WeatherForecast forecast);
    Task SaveBulkAsync(IReadOnlyDictionary<string, WeatherForecast> items);
    Task<IReadOnlyList<WeatherForecastStateEntry>> GetBulkAsync(IReadOnlyList<string> keys);
    Task<int> DeleteBulkAsync(IReadOnlyList<string> keys);
}