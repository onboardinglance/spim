using Spim.Domain.Entities;

namespace Spim.Application.Common.Interfaces;

public interface IWeatherForecastRepository
{
    Task<IReadOnlyList<WeatherForecast>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<WeatherForecast?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    void Add(WeatherForecast forecast);

    void Remove(WeatherForecast forecast);
}
