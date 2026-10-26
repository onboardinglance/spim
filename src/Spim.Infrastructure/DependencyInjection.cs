using Dapr.Client;
using Microsoft.Extensions.DependencyInjection;
using Spim.Application.Abstractions;
using Spim.Persistence.Dapr;

namespace Spim.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton(_ => new DaprClientBuilder().Build());
        services.AddScoped<IWeatherForecastStateStore, DaprWeatherForecastStateStore>();
        services.AddScoped<ITokenSessionStore, DaprTokenSessionStore>();
        return services;
    }
}
