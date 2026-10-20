using MediatR;
using Spim.Application.Common.Interfaces;
using Spim.Application.WeatherForecasts.Notifications;
using Spim.Domain.Entities;

namespace Spim.Application.WeatherForecasts.Commands.CreateWeatherForecast;

public sealed record CreateWeatherForecastCommand(DateOnly Date, int TemperatureC, string? Summary) : IRequest<int>;

public sealed class CreateWeatherForecastCommandHandler(
    IWeatherForecastRepository repository,
    IUnitOfWork unitOfWork,
    IPublisher publisher)
    : IRequestHandler<CreateWeatherForecastCommand, int>
{
    public async Task<int> Handle(CreateWeatherForecastCommand request, CancellationToken cancellationToken)
    {
        var forecast = WeatherForecast.Create(request.Date, request.TemperatureC, request.Summary);

        repository.Add(forecast);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new WeatherForecastCreatedNotification(forecast.Id, forecast.Date, forecast.Summary), cancellationToken);

        return forecast.Id;
    }
}
