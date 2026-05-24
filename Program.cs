using dapr_demo.Services;
using dapr_demo;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddDapr();
builder.Services.AddDaprClient();
builder.Services.AddScoped<TokenStateService>(); //registers service for dependency injection, ensureing only one instance of the service is created per HTTP request
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(options => 
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "oidc";
})
.AddCookie("Cookies", options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
})
.AddOpenIdConnect("oidc", options =>
{
    options.Authority = "http://localhost:8080/realms/demo";
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

if(!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
