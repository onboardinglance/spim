using System.Text.Json;
using Dapr.Client;
using Spim.Application.Abstractions;
using Spim.Application.State;
using Spim.Domain.Entities;

namespace Spim.Persistence.Dapr;

public sealed class DaprWeatherForecastStateStore(DaprClient daprClient) : IWeatherForecastStateStore
{
    private const string StateStore = "statestore";

    public Task SaveAsync(string key, WeatherForecast forecast) =>
        daprClient.SaveStateAsync(StateStore, key, ToSnapshot(forecast));

    public async Task<WeatherForecast?> GetAsync(string key)
    {
        var snapshot = await daprClient.GetStateAsync<WeatherForecastStateData>(StateStore, key);
        return snapshot is null ? null : ToEntity(snapshot);
    }

    public Task DeleteAsync(string key) => daprClient.DeleteStateAsync(StateStore, key);

    public Task ExecuteTransactionAsync(IReadOnlyList<StateWrite<WeatherForecast>> writes)
    {
        var operations = writes.Select(write => new StateTransactionRequest(
            write.Key,
            JsonSerializer.SerializeToUtf8Bytes(ToSnapshot(write.Value)),
            StateOperationType.Upsert)).ToList();

        return daprClient.ExecuteStateTransactionAsync(StateStore, operations);
    }

    public async Task<(WeatherForecast? Value, string? ETag)> GetWithETagAsync(string key)
    {
        var (snapshot, etag) = await daprClient.GetStateAndETagAsync<WeatherForecastStateData>(StateStore, key);
        return (snapshot is null ? null : ToEntity(snapshot), etag);
    }

    public Task<bool> TrySaveWithETagAsync(string key, WeatherForecast forecast, string? etag) =>
        daprClient.TrySaveStateAsync(StateStore, key, ToSnapshot(forecast), etag ?? string.Empty);

    public Task SaveWithTtlAsync(string key, WeatherForecast forecast, int ttlSeconds)
    {
        var metadata = new Dictionary<string, string> { ["ttlInSeconds"] = ttlSeconds.ToString() };
        return daprClient.SaveStateAsync(StateStore, key, ToSnapshot(forecast), metadata: metadata);
    }

    public Task SaveWithStrongConsistencyAsync(string key, WeatherForecast forecast)
    {
        var options = new StateOptions
        {
            Consistency = ConsistencyMode.Strong,
            Concurrency = ConcurrencyMode.LastWrite
        };

        return daprClient.SaveStateAsync(StateStore, key, ToSnapshot(forecast), stateOptions: options);
    }

    public async Task SaveBulkAsync(IReadOnlyDictionary<string, WeatherForecast> items)
    {
        foreach (var item in items)
            await daprClient.SaveStateAsync(StateStore, item.Key, ToSnapshot(item.Value));
    }

    public async Task<IReadOnlyList<WeatherForecastStateEntry>> GetBulkAsync(IReadOnlyList<string> keys)
    {
        var results = await daprClient.GetBulkStateAsync(StateStore, keys, parallelism: 3);
        return results.Select(item => new WeatherForecastStateEntry(item.Key, Deserialize(item.Value), item.ETag)).ToArray();
    }

    public async Task<int> DeleteBulkAsync(IReadOnlyList<string> keys)
    {
        var results = await daprClient.GetBulkStateAsync(StateStore, keys, parallelism: 3);
        var items = results.Select(result => new BulkDeleteStateItem(result.Key, result.ETag)).ToList();
        await daprClient.DeleteBulkStateAsync(StateStore, items);
        return items.Count;
    }

    private static WeatherForecast? Deserialize(object? value)
    {
        if (value is null)
            return null;
        if (value is WeatherForecast forecast)
            return forecast;
        if (value is JsonElement json)
        {
            var snapshot = json.Deserialize<WeatherForecastStateData>();
            return snapshot is null ? null : ToEntity(snapshot);
        }
        if (value is byte[] bytes)
        {
            var snapshot = JsonSerializer.Deserialize<WeatherForecastStateData>(bytes);
            return snapshot is null ? null : ToEntity(snapshot);
        }
        if (value is string text)
        {
            var snapshot = JsonSerializer.Deserialize<WeatherForecastStateData>(text);
            return snapshot is null ? null : ToEntity(snapshot);
        }

        var converted = JsonSerializer.Deserialize<WeatherForecastStateData>(JsonSerializer.Serialize(value));
        return converted is null ? null : ToEntity(converted);
    }

    private static WeatherForecastStateData ToSnapshot(WeatherForecast forecast) => new()
    {
        Date = forecast.Date,
        TemperatureC = forecast.TemperatureC,
        Summary = forecast.Summary,
        Created = forecast.Created,
        CreatedBy = forecast.CreatedBy,
        LastModified = forecast.LastModified,
        LastModifiedBy = forecast.LastModifiedBy
    };

    private static WeatherForecast ToEntity(WeatherForecastStateData snapshot)
    {
        var forecast = WeatherForecast.Create(snapshot.Date, snapshot.TemperatureC, snapshot.Summary);
        forecast.Created = snapshot.Created;
        forecast.CreatedBy = snapshot.CreatedBy;
        forecast.LastModified = snapshot.LastModified;
        forecast.LastModifiedBy = snapshot.LastModifiedBy;
        return forecast;
    }
}

public sealed class WeatherForecastStateData
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}