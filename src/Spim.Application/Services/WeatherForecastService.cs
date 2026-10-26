using Spim.Application.Abstractions;
using Spim.Application.Models;
using Spim.Application.State;
using Spim.Domain.Entities;

namespace Spim.Application.Services;

public sealed class WeatherForecastService(IWeatherForecastStateStore stateStore)
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public IReadOnlyList<WeatherForecastData> GetForecasts()
    {
        return Enumerable.Range(1, 5).Select(index =>
        {
            var temperatureC = Random.Shared.Next(-20, 55);
            var summary = Summaries[Random.Shared.Next(Summaries.Length)];
            var forecast = WeatherForecast.Create(
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                temperatureC,
                summary);
            return ToData(forecast)!;
        }).ToArray();
    }

    public Task SaveAsync(string key, WeatherForecastData forecast) =>
        stateStore.SaveAsync(key, ToEntity(forecast));

    public async Task<WeatherForecastData?> GetAsync(string key) => ToData(await stateStore.GetAsync(key));

    public Task DeleteAsync(string key) => stateStore.DeleteAsync(key);

    public async Task<IReadOnlyList<string>> SaveTransactionAsync(IReadOnlyList<WeatherForecastData> forecasts)
    {
        var writes = forecasts.Select((forecast, index) =>
            new StateWrite<WeatherForecast>($"tx-key-{index}", ToEntity(forecast))).ToArray();
        await stateStore.ExecuteTransactionAsync(writes);
        return writes.Select(write => write.Key).ToArray();
    }

    public async Task<(WeatherForecastData? Value, string? ETag)> GetWithETagAsync(string key)
    {
        var (value, etag) = await stateStore.GetWithETagAsync(key);
        return (ToData(value), etag);
    }

    public Task<bool> TrySaveWithETagAsync(string key, WeatherForecastData forecast, string? etag) =>
        stateStore.TrySaveWithETagAsync(key, ToEntity(forecast), etag);

    public Task SaveWithTtlAsync(string key, WeatherForecastData forecast, int ttlSeconds) =>
        stateStore.SaveWithTtlAsync(key, ToEntity(forecast), ttlSeconds);

    public Task SaveWithStrongConsistencyAsync(string key, WeatherForecastData forecast) =>
        stateStore.SaveWithStrongConsistencyAsync(key, ToEntity(forecast));

    public Task SaveBulkAsync(IReadOnlyDictionary<string, WeatherForecastData> items) =>
        stateStore.SaveBulkAsync(items.ToDictionary(item => item.Key, item => ToEntity(item.Value)));

    public async Task<IReadOnlyList<WeatherForecastDataEntry>> GetBulkAsync(IReadOnlyList<string> keys)
    {
        var entries = await stateStore.GetBulkAsync(keys);
        return entries.Select(entry => new WeatherForecastDataEntry(entry.Key, ToData(entry.Value), entry.ETag)).ToArray();
    }

    public Task<int> DeleteBulkAsync(IReadOnlyList<string> keys) => stateStore.DeleteBulkAsync(keys);

    private static WeatherForecast ToEntity(WeatherForecastData data) =>
        WeatherForecast.Create(DateOnly.FromDateTime(data.Date), data.TemperatureC, data.Summary);

    private static WeatherForecastData? ToData(WeatherForecast? forecast) => forecast is null
        ? null
        : new WeatherForecastData(forecast.Date.ToDateTime(TimeOnly.MinValue), forecast.TemperatureC, forecast.Summary);
}