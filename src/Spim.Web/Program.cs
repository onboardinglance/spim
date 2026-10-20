using Autofac;
using Autofac.Extensions.DependencyInjection;
using Serilog;
using Spim.Application;
using Spim.Infrastructure;
using Spim.Persistence;
using Spim.Web;
using Spim.Web.OpenApi;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, logger) => logger
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    builder.Host.ConfigureContainer<ContainerBuilder>(container =>
    {
        container.RegisterModule<InfrastructureModule>();
        container.RegisterModule<PersistenceModule>();
        container.RegisterModule<WebModule>();
    });

    builder.Services
        .AddApplicationServices(builder.Configuration)
        .AddInfrastructureServices(builder.Configuration)
        .AddPersistenceServices(builder.Configuration)
        .AddWebServices(builder.Configuration);

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        await app.Services.InitialiseDatabaseAsync();
    }

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();

    app.UseCors(Spim.Web.DependencyInjection.CorsPolicyName);
    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.MapApiDocumentation();
    }

    app.MapControllers();
    app.MapHealthChecks("/health/ready").AllowAnonymous();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
