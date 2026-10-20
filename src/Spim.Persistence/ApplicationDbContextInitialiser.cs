using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spim.Domain.Entities;

namespace Spim.Persistence;

public sealed class ApplicationDbContextInitialiser(ApplicationDbContext context, ILogger<ApplicationDbContextInitialiser> logger)
{
    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        if (context.Database.GetMigrations().Any())
        {
            logger.LogInformation("Applying database migrations");
            await context.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            logger.LogWarning("No migrations found; creating schema with EnsureCreated");
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await context.WeatherForecasts.AnyAsync(cancellationToken))
            return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var forecasts = Enumerable.Range(1, 5)
            .Select(i => WeatherForecast.Create(today.AddDays(i), Random.Shared.Next(-20, 55), null));

        context.WeatherForecasts.AddRange(forecasts);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded sample weather forecasts");
    }
}

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync(cancellationToken);
        await initialiser.SeedAsync(cancellationToken);
    }
}
