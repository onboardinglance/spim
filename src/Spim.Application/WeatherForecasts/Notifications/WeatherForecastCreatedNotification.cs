using MediatR;
using Spim.Application.Common.Interfaces;

namespace Spim.Application.WeatherForecasts.Notifications;

public sealed record WeatherForecastCreatedNotification(int Id, DateOnly Date, string? Summary) : INotification;

public sealed class SendWeatherForecastCreatedEmailHandler(IEmailService emailService, ICurrentUserService currentUser)
    : INotificationHandler<WeatherForecastCreatedNotification>
{
    public Task Handle(WeatherForecastCreatedNotification notification, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentUser.Email))
            return Task.CompletedTask;

        return emailService.SendAsync(
            currentUser.Email,
            $"Weather forecast #{notification.Id} created",
            $"A forecast for {notification.Date:yyyy-MM-dd} ({notification.Summary}) was created.",
            cancellationToken);
    }
}
