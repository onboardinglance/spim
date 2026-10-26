using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spim.Application.Models;
using Spim.Application.Services;

namespace Spim.Web.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class WeatherForecastController(WeatherForecastService weatherForecastService) : ControllerBase
{
    [HttpPost("{key}")]
    public Task SaveState(string key, [FromBody] WeatherForecastData forecast) =>
        weatherForecastService.SaveAsync(key, forecast);

    [HttpGet("{key}")]
    public Task<WeatherForecastData?> GetState(string key) => weatherForecastService.GetAsync(key);

    [HttpDelete("{key}")]
    public Task DeleteState(string key) => weatherForecastService.DeleteAsync(key);

    [HttpGet(Name = "GetWeatherForecast")]
    public IReadOnlyList<WeatherForecastData> Get() => weatherForecastService.GetForecasts();

    [HttpPost("transaction")]
    public async Task<IActionResult> SaveTransaction([FromBody] List<WeatherForecastData> forecasts)
    {
        var keys = await weatherForecastService.SaveTransactionAsync(forecasts);
        return Ok(new { message = "Transaction saved", keys });
    }

    [HttpGet("etag/{key}")]
    public async Task<IActionResult> GetWithETag(string key)
    {
        var (value, etag) = await weatherForecastService.GetWithETagAsync(key);
        return Ok(new { value, etag });
    }

    [HttpPut("etag/{key}")]
    public async Task<IActionResult> SaveWithETag(string key, [FromBody] ETagRequest request)
    {
        var saved = await weatherForecastService.TrySaveWithETagAsync(key, request.Forecast, request.ETag);
        return Ok(new { saved, message = saved ? "Saved successfully" : "Conflict: data changed since last read" });
    }

    [HttpPost("ttl/{key}")]
    public async Task<IActionResult> SaveWithTTL(
        string key,
        [FromBody] WeatherForecastData forecast,
        [FromQuery] int ttlSeconds = 30)
    {
        await weatherForecastService.SaveWithTtlAsync(key, forecast, ttlSeconds);
        return Ok(new { message = $"Saved with TTL of {ttlSeconds}s; key will auto-expire" });
    }

    [HttpPost("strong/{key}")]
    public async Task<IActionResult> SaveWithStrongConsistency(string key, [FromBody] WeatherForecastData forecast)
    {
        await weatherForecastService.SaveWithStrongConsistencyAsync(key, forecast);
        return Ok(new { message = "Saved with strong consistency" });
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> SaveBulk([FromBody] Dictionary<string, WeatherForecastData> items)
    {
        await weatherForecastService.SaveBulkAsync(items);
        return Ok(new { message = $"Saved {items.Count} items" });
    }

    [HttpGet("bulk")]
    public async Task<IActionResult> GetBulk([FromQuery] string keys)
    {
        var results = await weatherForecastService.GetBulkAsync(keys.Split(',').ToArray());
        return Ok(results);
    }

    [HttpDelete("bulk")]
    public async Task<IActionResult> DeleteBulk([FromQuery] string keys)
    {
        var deletedCount = await weatherForecastService.DeleteBulkAsync(keys.Split(',').ToArray());
        return Ok(new { message = $"Deleted {deletedCount} keys" });
    }

    [Authorize]
    [HttpGet("my-token")]
    public async Task<IActionResult> GetMyToken([FromServices] TokenSessionService tokenService)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "User identifier missing from token" });

        var session = await tokenService.GetTokenAsync(userId);
        if (session is null || session.IsExpired)
            return Unauthorized(new { message = "Session expired, please login again" });

        return Ok(new { session.AccessToken, session.ExpiresAt });
    }
}

public sealed class ETagRequest
{
    public WeatherForecastData Forecast { get; set; } = null!;
    public string ETag { get; set; } = null!;
}