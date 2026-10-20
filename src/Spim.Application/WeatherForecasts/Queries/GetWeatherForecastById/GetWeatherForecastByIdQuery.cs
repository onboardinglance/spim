using AutoMapper;
using MediatR;
using Spim.Application.Common.Exceptions;
using Spim.Application.Common.Interfaces;
using Spim.Domain.Entities;

namespace Spim.Application.WeatherForecasts.Queries.GetWeatherForecastById;

public sealed record GetWeatherForecastByIdQuery(int Id) : IRequest<WeatherForecastDto>;

public sealed class GetWeatherForecastByIdQueryHandler(IWeatherForecastRepository repository, IMapper mapper)
    : IRequestHandler<GetWeatherForecastByIdQuery, WeatherForecastDto>
{
    public async Task<WeatherForecastDto> Handle(GetWeatherForecastByIdQuery request, CancellationToken cancellationToken)
    {
        var forecast = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WeatherForecast), request.Id);

        return mapper.Map<WeatherForecastDto>(forecast);
    }
}
