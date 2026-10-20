namespace Spim.Web.Authentication;

public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    /// <summary>Realm URL the API uses to fetch signing keys (may be an internal cluster address).</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Browser-facing realm URL used by Scalar; defaults to <see cref="Authority"/>.</summary>
    public string? PublicAuthority { get; set; }

    public string? Audience { get; set; }

    /// <summary>Public (PKCE) Keycloak client used by the Scalar UI.</summary>
    public string ClientId { get; set; } = string.Empty;

    public bool RequireHttpsMetadata { get; set; } = true;

    public string[] Scopes { get; set; } = ["openid", "profile", "email"];

    public string BrowserAuthority => string.IsNullOrWhiteSpace(PublicAuthority) ? Authority : PublicAuthority;
}
