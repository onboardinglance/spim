using Microsoft.Extensions.DependencyInjection;
using Spim.Application.Services;

namespace Spim.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<WeatherForecastService>();
        services.AddScoped<TokenSessionService>();
        return services;
    }
}
