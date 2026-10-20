using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Spim.Web.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(KeycloakOptions.SectionName);
        services.Configure<KeycloakOptions>(section);

        var keycloak = section.Get<KeycloakOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{KeycloakOptions.SectionName}' is missing.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = keycloak.Authority;
                options.RequireHttpsMetadata = keycloak.RequireHttpsMetadata;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Tokens issued via the public URL (e.g. port-forward) carry a different `iss` than the internal authority.
                    ValidIssuers = new[] { keycloak.Authority, keycloak.BrowserAuthority }.Distinct(),
                    ValidateAudience = !string.IsNullOrWhiteSpace(keycloak.Audience),
                    ValidAudience = keycloak.Audience,
                    NameClaimType = "preferred_username"
                };
            });

        return services;
    }
}
