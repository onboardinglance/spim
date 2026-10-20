using Microsoft.EntityFrameworkCore;
using Spim.Application.Common.Interfaces;
using Spim.Domain.Entities;

namespace Spim.Persistence.Repositories;

public sealed class WeatherForecastRepository(ApplicationDbContext context) : IWeatherForecastRepository
{
    public async Task<IReadOnlyList<WeatherForecast>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.WeatherForecasts
            .AsNoTracking()
            .OrderBy(f => f.Date)
            .ToListAsync(cancellationToken);

    public Task<WeatherForecast?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        context.WeatherForecasts.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public void Add(WeatherForecast forecast) => context.WeatherForecasts.Add(forecast);

    public void Remove(WeatherForecast forecast) => context.WeatherForecasts.Remove(forecast);
}
