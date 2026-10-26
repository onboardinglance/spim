using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Spim.Application;
using Spim.Application.Services;
using Spim.Domain.Entities;
using Spim.Infrastructure;
using Spim.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var authority = builder.Configuration["Oidc:Authority"] ?? "http://localhost:8081/realms/demo";
var metadataAddress = builder.Configuration["Oidc:MetadataAddress"]
    ?? "http://localhost:8081/realms/demo/.well-known/openid-configuration";

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "SmartScheme";
    options.DefaultChallengeScheme = "oidc";
})
.AddPolicyScheme("SmartScheme", "Cookies or JWT", options =>
{
    options.ForwardDefaultSelector = context =>
        context.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? JwtBearerDefaults.AuthenticationScheme
            : "Cookies";
})
.AddCookie("Cookies", options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
})
.AddJwtBearer(options =>
{
    options.Authority = authority;
    options.MetadataAddress = metadataAddress;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = false };
})
.AddOpenIdConnect("oidc", options =>
{
    options.Authority = authority;
    options.MetadataAddress = metadataAddress;
    options.ClientId = "dapr-demo";
    options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];
    options.ResponseType = "code";
    options.RequireHttpsMetadata = false;
    options.SaveTokens = false;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.CallbackPath = "/signin-oidc";
    options.MapInboundClaims = false;
    options.CorrelationCookie.SameSite = SameSiteMode.Unspecified;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.None;
    options.NonceCookie.SameSite = SameSiteMode.Unspecified;
    options.NonceCookie.SecurePolicy = CookieSecurePolicy.None;
    options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = false };

    options.Events.OnTokenValidated = async context =>
    {
        var tokenService = context.HttpContext.RequestServices.GetRequiredService<TokenSessionService>();
        var userId = context.Principal!.FindFirst("sub")?.Value
            ?? context.Principal.FindFirst("upn")?.Value
            ?? throw new InvalidOperationException("No user identifier in token");
        var expiresIn = int.TryParse(context.TokenEndpointResponse?.ExpiresIn?.ToString(), out var seconds)
            ? seconds
            : 3600;

        await tokenService.StoreTokenAsync(new TokenSession
        {
            UserId = userId,
            AccessToken = context.TokenEndpointResponse!.AccessToken!,
            RefreshToken = context.TokenEndpointResponse.RefreshToken!,
            IdToken = context.TokenEndpointResponse.IdToken!,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn)
        });
    };

    options.Events.OnSignedOutCallbackRedirect = async context =>
    {
        var tokenService = context.HttpContext.RequestServices.GetRequiredService<TokenSessionService>();
        var userId = context.HttpContext.User.FindFirst("sub")?.Value;
        if (userId is not null)
            await tokenService.RevokeTokenAsync(userId);
        context.HandleResponse();
        context.Response.Redirect("/");
    };

    options.Events.OnRedirectToIdentityProvider = context =>
    {
        if (context.ProtocolMessage.IssuerAddress.Contains("keycloak-service:8080"))
        {
            context.ProtocolMessage.IssuerAddress = context.ProtocolMessage.IssuerAddress
                .Replace("keycloak-service:8080", "localhost:8081");
        }
        return Task.CompletedTask;
    };

    options.Events.OnRemoteFailure = context =>
    {
        context.Response.Redirect("/?error=" + context.Failure?.Message);
        context.HandleResponse();
        return Task.CompletedTask;
    };
});

var app = builder.Build();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/ready");

app.Run();
