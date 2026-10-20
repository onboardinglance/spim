using MediatR;
using Spim.Application.Common.Exceptions;
using Spim.Application.Common.Interfaces;
using Spim.Domain.Entities;

namespace Spim.Application.WeatherForecasts.Commands.UpdateWeatherForecast;

public sealed record UpdateWeatherForecastCommand(int Id, DateOnly Date, int TemperatureC, string? Summary) : IRequest;

public sealed class UpdateWeatherForecastCommandHandler(IWeatherForecastRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateWeatherForecastCommand>
{
    public async Task Handle(UpdateWeatherForecastCommand request, CancellationToken cancellationToken)
    {
        var forecast = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WeatherForecast), request.Id);

        forecast.Update(request.Date, request.TemperatureC, request.Summary);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
