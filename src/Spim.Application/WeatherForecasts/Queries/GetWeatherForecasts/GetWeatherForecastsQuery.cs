using AutoMapper;
using MediatR;
using Spim.Application.Common.Interfaces;

namespace Spim.Application.WeatherForecasts.Queries.GetWeatherForecasts;

public sealed record GetWeatherForecastsQuery : IRequest<IReadOnlyList<WeatherForecastDto>>;

public sealed class GetWeatherForecastsQueryHandler(IWeatherForecastRepository repository, IMapper mapper)
    : IRequestHandler<GetWeatherForecastsQuery, IReadOnlyList<WeatherForecastDto>>
{
    public async Task<IReadOnlyList<WeatherForecastDto>> Handle(GetWeatherForecastsQuery request, CancellationToken cancellationToken)
    {
        var forecasts = await repository.GetAllAsync(cancellationToken);
        return mapper.Map<List<WeatherForecastDto>>(forecasts);
    }
}
