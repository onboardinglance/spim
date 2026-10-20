using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Spim.Web.Authentication;

namespace Spim.Web.OpenApi;

public static class ApiDocumentationExtensions
{
    public const string SecuritySchemeName = "Keycloak";

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(options => options.AddDocumentTransformer<KeycloakSecuritySchemeTransformer>());
        return services;
    }

    public static WebApplication MapApiDocumentation(this WebApplication app)
    {
        var keycloak = app.Services.GetRequiredService<IOptions<KeycloakOptions>>().Value;

        app.MapOpenApi().AllowAnonymous();
        app.MapScalarApiReference(options => options
            .WithTitle("Spim API")
            .AddPreferredSecuritySchemes(SecuritySchemeName)
            .AddAuthorizationCodeFlow(SecuritySchemeName, flow =>
            {
                flow.ClientId = keycloak.ClientId;
                flow.Pkce = Pkce.Sha256;
                flow.SelectedScopes = keycloak.Scopes;
            }))
            .AllowAnonymous();

        return app;
    }
}
