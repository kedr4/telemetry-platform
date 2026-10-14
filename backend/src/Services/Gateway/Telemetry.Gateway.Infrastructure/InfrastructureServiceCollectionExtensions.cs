using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telemetry.BuildingBlocks.Health;
using Telemetry.Gateway.Application.Abstractions;
using Telemetry.Gateway.Infrastructure.Persistence;

namespace Telemetry.Gateway.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public const string ConnectionStringName = "Telemetry";

    public static IServiceCollection AddGatewayInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");
        }

        services.AddDbContext<TelemetryReadDbContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3))
            .UseSnakeCaseNamingConvention()
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.AddScoped<ITelemetryReadStore, TelemetryReadStore>();

        services.AddHealthChecks()
            .AddDbContextCheck<TelemetryReadDbContext>("postgres", tags: [HealthCheckTags.Ready]);

        return services;
    }
}
