using MediatR;
using Spim.Application.Common.Exceptions;
using Spim.Application.Common.Interfaces;
using Spim.Domain.Entities;

namespace Spim.Application.WeatherForecasts.Commands.DeleteWeatherForecast;

public sealed record DeleteWeatherForecastCommand(int Id) : IRequest;

public sealed class DeleteWeatherForecastCommandHandler(IWeatherForecastRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteWeatherForecastCommand>
{
    public async Task Handle(DeleteWeatherForecastCommand request, CancellationToken cancellationToken)
    {
        var forecast = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WeatherForecast), request.Id);

        repository.Remove(forecast);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
