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
    options.DefaultScheme = "Cookies";  // to store the authenticated session as cookies
    options.DefaultChallengeScheme = "oidc";
})
.AddCookie("Cookies")
.AddOpenIdConnect("oidc", options =>

{
    options.Authority = "https://your-keycloak/realms/your-realm";
    // ADFS
    // options.Authority = "https://your-adfs/adfs";

    options.ClientId = "your-client-id";
    options.ClientSecret = "your-client-secret";
    options.ResponseType = "code";
    options.SaveTokens = false;          // We store in Dapr, not cookies
    options.GetClaimsFromUserInfoEndpoint = true;

    options.Events.OnTokenValidated = async ctx =>
    {
        var tokenService = ctx.HttpContext.RequestServices.GetRequiredService<TokenStateService>();
        
        var userId = ctx.Principal!.FindFirst("sub")?.Value //finds sub first if not then upn else throw error
        ?? ctx.Principal!.FindFirst("upn")?.Value
        ?? throw new InvalidOperationException("No user identifier in token");

        var expiresIn = ctx.TokenEndpointResponse?.ExpiresIn ?? 3600;
        
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
        if(userId != null)
        { await tokenService.RevokeTokenAsync(userId); }
        ctx.HandleResponse();
        ctx.Response.Redirect("/");
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

app.UseAuthorization();

app.MapControllers();

app.Run();
