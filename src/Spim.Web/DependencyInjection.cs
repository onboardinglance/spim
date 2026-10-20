using Spim.Web.Authentication;
using Spim.Web.Infrastructure;
using Spim.Web.OpenApi;

namespace Spim.Web;

public static class DependencyInjection
{
    public const string CorsPolicyName = "Default";

    public static IServiceCollection AddWebServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddControllers();

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddKeycloakAuthentication(configuration);
        services.AddAuthorization();

        services.AddApiDocumentation();

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy(CorsPolicyName, policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        return services;
    }
}
