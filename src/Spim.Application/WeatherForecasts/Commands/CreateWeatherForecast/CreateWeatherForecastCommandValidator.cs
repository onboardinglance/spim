using FluentValidation;
using Spim.Domain.Entities;

namespace Spim.Application.WeatherForecasts.Commands.CreateWeatherForecast;

public sealed class CreateWeatherForecastCommandValidator : AbstractValidator<CreateWeatherForecastCommand>
{
    public CreateWeatherForecastCommandValidator()
    {
        RuleFor(c => c.Date).NotEmpty();
        RuleFor(c => c.TemperatureC).InclusiveBetween(WeatherForecast.MinTemperatureC, WeatherForecast.MaxTemperatureC);
        RuleFor(c => c.Summary).MaximumLength(WeatherForecast.SummaryMaxLength);
    }
}
