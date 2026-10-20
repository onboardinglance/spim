using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Spim.Web.Authentication;

namespace Spim.Web.OpenApi;

internal sealed class KeycloakSecuritySchemeTransformer(IOptions<KeycloakOptions> options) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var keycloak = options.Value;
        var realm = keycloak.BrowserAuthority.TrimEnd('/');

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[ApiDocumentationExtensions.SecuritySchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = "Keycloak OAuth2 authorization code flow with PKCE",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{realm}/protocol/openid-connect/auth"),
                    TokenUrl = new Uri($"{realm}/protocol/openid-connect/token"),
                    RefreshUrl = new Uri($"{realm}/protocol/openid-connect/token"),
                    Scopes = keycloak.Scopes.ToDictionary(s => s, s => s)
                }
            }
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(ApiDocumentationExtensions.SecuritySchemeName, document)] = [.. keycloak.Scopes]
        });

        return Task.CompletedTask;
    }
}
