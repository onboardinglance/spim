using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Spim.Domain.Entities;

namespace Spim.Persistence.Configurations;

public sealed class WeatherForecastConfiguration : IEntityTypeConfiguration<WeatherForecast>
{
    public void Configure(EntityTypeBuilder<WeatherForecast> builder)
    {
        builder.ToTable("weather_forecasts");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).UseIdentityByDefaultColumn();

        builder.Property(f => f.Date).IsRequired();
        builder.Property(f => f.TemperatureC).IsRequired();
        builder.Property(f => f.Summary).HasMaxLength(WeatherForecast.SummaryMaxLength);

        builder.Property(f => f.CreatedBy).HasMaxLength(256);
        builder.Property(f => f.LastModifiedBy).HasMaxLength(256);

        builder.Ignore(f => f.TemperatureF);

        builder.HasIndex(f => f.Date);
    }
}
