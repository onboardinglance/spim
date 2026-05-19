using Microsoft.AspNetCore.Mvc;
using Dapr.Client;
using System.Text.Json;

namespace dapr_demo.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly DaprClient _daprClient;
    private const string StateStore = "statestore";
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, DaprClient daprClient)
    {
        _daprClient = daprClient;
        _logger = logger;
    }

    [HttpPost("{key}")]
    public async Task SaveState(string key, [FromBody] WeatherForecast forecast)
    {
        await _daprClient.SaveStateAsync(StateStore, key, forecast);
    }

    [HttpGet("{key}")]
    public async Task<WeatherForecast?> GetState(string key)
    {
        return await _daprClient.GetStateAsync<WeatherForecast>(StateStore, key);
    }

    [HttpDelete("{key}")]
    public async Task DeleteState(string key)
    {
        await _daprClient.DeleteStateAsync(StateStore, key);
    }


    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }

    // ── 1. Transaction ─────────────────────
    // retrieve bulk through postman use GET http://localhost:5157/WeatherForecast/bulk?keys=tx-key-0,tx-key-1
    // retrieve single through postman use GET the tx-key as part of the url GET http://localhost:5157/WeatherForecast/tx-key-0

    [HttpPost("transaction")]
    public async Task<IActionResult> SaveTransaction([FromBody] List<WeatherForecast> forecasts)
    {
        var operations = forecasts.Select((f, i) =>
            new StateTransactionRequest(
                $"tx-key-{i}",
                JsonSerializer.SerializeToUtf8Bytes(f),
                StateOperationType.Upsert)
        ).ToList<StateTransactionRequest>();

        await _daprClient.ExecuteStateTransactionAsync(StateStore, operations);
        return Ok(new { message = "Transaction saved", keys = forecasts.Select((_, i) => $"tx-key-{i}") });
    }

    // ── 2. ETags (Optimistic Concurrency) ─────────────────────
    // every update, dapr will push up the etag value by 1
    // to do the update, need to use the latest, else will result in conflict error and unable to continue
    // This is optimistic concurrency control, would it be possible to use this to control the messaging order issue 


    //Eg:  PUT http://localhost:5157/WeatherForecast/etag/mykey
    // { "forecast": { "date": "2026-05-17", "temperatureC": 25, "summary": "Warm" },
    // "etag": "<paste etag here>" }
    
    [HttpGet("etag/{key}")]
    public async Task<IActionResult> GetWithETag(string key)
    {
        var (value, etag) = await _daprClient.GetStateAndETagAsync<WeatherForecast>(StateStore, key);
        return Ok(new { value, etag });
    }

    [HttpPut("etag/{key}")]
    public async Task<IActionResult> SaveWithETag(string key, [FromBody] ETagRequest request)
    {
        var saved = await _daprClient.TrySaveStateAsync(StateStore, key, request.Forecast, request.ETag);
        return Ok(new { saved, message = saved ? "Saved successfully" : "Conflict — data changed since last read" });
    }

     // ── 3. TTL ────────────────────────────────────────────────
     // POST http://localhost:5157/WeatherForecast/ttl/tempkey?ttlSeconds=10
     // // { "date": "2026-05-17", "temperatureC": 20, "summary": "Mild" }

    [HttpPost("ttl/{key}")]
    public async Task<IActionResult> SaveWithTTL(string key, [FromBody] WeatherForecast forecast, [FromQuery] int ttlSeconds = 30)
    {
        var metadata = new Dictionary<string, string> { { "ttlInSeconds", ttlSeconds.ToString() } };
        await _daprClient.SaveStateAsync(StateStore, key, forecast, metadata: metadata);
        return Ok(new { message = $"Saved with TTL of {ttlSeconds}s — key will auto-expire" });
    }

    // ── 4. Consistency ────────────────────────────────────────
    // basically it guarantees that any read opertaion immediately returns the latest, most up to date data following a write
    // POST http://localhost:5157/WeatherForecast/strong/mykey
    // { "date": "2026-05-17", "temperatureC": 20, "summary": "Mild" }
    [HttpPost("strong/{key}")]
    public async Task<IActionResult> SaveWithStrongConsistency(string key, [FromBody] WeatherForecast forecast)
    {
        var options = new StateOptions
        {
            Consistency = ConsistencyMode.Strong,
            Concurrency = ConcurrencyMode.LastWrite
        };
        await _daprClient.SaveStateAsync(StateStore, key, forecast, stateOptions: options);
        return Ok(new { message = "Saved with strong consistency" });
    }

    // ── 5. Bulk Operations ────────────────────────────────────
    // POST http://localhost:5157/WeatherForecast/bulk {
    // "key1": { "date": "2026-05-17", "temperatureC": 20, "summary": "Mild" },
    //"key2": { "date": "2026-05-18", "temperatureC": 30, "summary": "Hot" },
    //"key3": { "date": "2026-05-19", "temperatureC": 10, "summary": "Chilly" }
   //}
    
    // GET http://localhost:5157/WeatherForecast/bulk?keys=key1,key2,key3
    // DELETE http://localhost:5157/WeatherForecast/bulk?keys=key1,key2,key3d
    [HttpPost("bulk")]
    public async Task<IActionResult> SaveBulk([FromBody] Dictionary<string, WeatherForecast> items)
    {
        foreach (var item in items)
            await _daprClient.SaveStateAsync(StateStore, item.Key, item.Value);

        return Ok(new { message = $"Saved {items.Count} items" });
    }

    [HttpGet("bulk")]
    public async Task<IActionResult> GetBulk([FromQuery] string keys)
    {
        var keyList = keys.Split(',').ToList();
        var results = await _daprClient.GetBulkStateAsync(StateStore, keyList, parallelism: 3);
        return Ok(results.Select(r => new { r.Key, r.Value, r.ETag }));
    }

    [HttpDelete("bulk")]
    public async Task<IActionResult> DeleteBulk([FromQuery] string keys)
    {
        var keyList = keys.Split(',').ToList();
        var results = await _daprClient.GetBulkStateAsync(StateStore, keyList, parallelism: 3);
        var deleteItems = results.Select(r => new BulkDeleteStateItem(r.Key, r.ETag)).ToList();
        await _daprClient.DeleteBulkStateAsync(StateStore, deleteItems);
        return Ok(new { message = $"Deleted {deleteItems.Count} keys" });
    }

    [Authorize]
    [HttpGet("my-token")]
    public async Task<IActionResult> GetMyToken([FromServices] TokenStateService tokenService)
    {
        var userId = User.FindFirst("sub")?.Value!;
        var session = await tokenService.GetTokenAsync(userId);

        if (session == null || session.IsExpired)
            return Unauthorized(new { message = "Session expired, please login again" });

        return Ok(new { session.AccessToken, session.ExpiresAt });
    }
}

// ── Supporting model for ETag endpoint ────────────────────────
public class ETagRequest
{
    public WeatherForecast Forecast { get; set; } = null!;
    public string ETag { get; set; } = null!;
}


