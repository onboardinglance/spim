using AutoMapper;
using Spim.Domain.Entities;

namespace Spim.Application.WeatherForecasts;

public sealed class WeatherForecastProfile : Profile
{
    public WeatherForecastProfile()
    {
        CreateMap<WeatherForecast, WeatherForecastDto>();
    }
}
