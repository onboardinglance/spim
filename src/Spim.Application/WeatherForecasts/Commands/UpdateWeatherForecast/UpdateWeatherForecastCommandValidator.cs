using FluentValidation;
using Spim.Domain.Entities;

namespace Spim.Application.WeatherForecasts.Commands.UpdateWeatherForecast;

public sealed class UpdateWeatherForecastCommandValidator : AbstractValidator<UpdateWeatherForecastCommand>
{
    public UpdateWeatherForecastCommandValidator()
    {
        RuleFor(c => c.Id).GreaterThan(0);
        RuleFor(c => c.Date).NotEmpty();
        RuleFor(c => c.TemperatureC).InclusiveBetween(WeatherForecast.MinTemperatureC, WeatherForecast.MaxTemperatureC);
        RuleFor(c => c.Summary).MaximumLength(WeatherForecast.SummaryMaxLength);
    }
}
