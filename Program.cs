using dapr_demo.Services;
using dapr_demo;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Enable PII to see the actual connection errors (e.g., 404 vs 503) in the logs
Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

// Add services to the container.

builder.Services.AddControllers().AddDapr();
builder.Services.AddDaprClient();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<TokenStateService>(); //registers service for dependency injection, ensureing only one instance of the service is created per HTTP request
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Dapr Demo API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddAuthentication(options => 
{
    options.DefaultScheme = "SmartScheme";
    options.DefaultChallengeScheme = "oidc";
})
.AddPolicyScheme("SmartScheme", "Cookies or JWT", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        var authHeader = context.Request.Headers["Authorization"].ToString();
        return authHeader.StartsWith("Bearer ") ? JwtBearerDefaults.AuthenticationScheme : "Cookies";
    };
})
.AddCookie("Cookies", options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
})
.AddJwtBearer(options =>
{
    options.Authority = builder.Configuration["Oidc:Authority"] ?? "http://localhost:8081/realms/demo";
    options.MetadataAddress = builder.Configuration["Oidc:MetadataAddress"] ?? "http://keycloak-service:8080/realms/demo/.well-known/openid-configuration";
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false
    };
})
.AddOpenIdConnect("oidc", options =>
{
    // Authority: Where the BROWSER goes (Localhost via port-forward)
    options.Authority = builder.Configuration["Oidc:Authority"] ?? "http://localhost:8081/realms/demo";
    
    // MetadataAddress: Where the BACKEND goes (Internal K8s DNS)
    options.MetadataAddress = builder.Configuration["Oidc:MetadataAddress"] ?? "http://keycloak-service:8080/realms/demo/.well-known/openid-configuration";

    options.ClientId = "dapr-demo";
    options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];
    options.ResponseType = "code";
    options.RequireHttpsMetadata = false;
    options.SaveTokens = false;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.CallbackPath = "/signin-oidc";  //set this in your keycloak redirect URIs
    options.MapInboundClaims = false;
    options.CorrelationCookie.SameSite = SameSiteMode.Unspecified;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.None;
    options.NonceCookie.SameSite = SameSiteMode.Unspecified;
    options.NonceCookie.SecurePolicy = CookieSecurePolicy.None;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false // Required because internal service name != localhost:8081
    };

    options.Events.OnTokenValidated = async ctx =>
    {
        var tokenService = ctx.HttpContext.RequestServices.GetRequiredService<TokenStateService>();
        var userId = ctx.Principal!.FindFirst("sub")?.Value
            ?? ctx.Principal!.FindFirst("upn")?.Value
            ?? throw new InvalidOperationException("No user identifier in token");
        var expiresIn = int.TryParse(ctx.TokenEndpointResponse?.ExpiresIn?.ToString(), out var exp) ? exp : 3600;
        var session = new TokenSession
        {
            UserId = userId,
            AccessToken = ctx.TokenEndpointResponse!.AccessToken,
            RefreshToken = ctx.TokenEndpointResponse.RefreshToken,
            IdToken = ctx.TokenEndpointResponse.IdToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn)
        };
        await tokenService.StoreTokenAsync(session);
    };

    options.Events.OnSignedOutCallbackRedirect = async ctx =>
    {
        var tokenService = ctx.HttpContext.RequestServices.GetRequiredService<TokenStateService>();
        var userId = ctx.HttpContext.User.FindFirst("sub")?.Value;
        if (userId != null)
            await tokenService.RevokeTokenAsync(userId);
        ctx.HandleResponse();
        ctx.Response.Redirect("/");
    };

    options.Events.OnRedirectToIdentityProvider = context =>
    {
        // Fix for port-forwarding: The internal metadata discovery returns the internal K8s DNS, 
        // but the browser (external) needs to use localhost:8081.
        if (context.ProtocolMessage.IssuerAddress.Contains("keycloak-service:8080"))
        {
            context.ProtocolMessage.IssuerAddress = context.ProtocolMessage.IssuerAddress
                .Replace("keycloak-service:8080", "localhost:8081");
        }
        return Task.CompletedTask;
    };

    options.Events.OnRemoteFailure = ctx =>
    {
        ctx.Response.Redirect("/?error=" + ctx.Failure?.Message);
        ctx.HandleResponse();
        return Task.CompletedTask;
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// Commenting out prevents "invalid protocol" errors during port-forwarding
// if(!app.Environment.IsDevelopment())
// {
//     app.UseHttpsRedirection();
// }

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/ready");

app.Run();
