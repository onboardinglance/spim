using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spim.Infrastructure.Email;

namespace Spim.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        return services;
    }
}
