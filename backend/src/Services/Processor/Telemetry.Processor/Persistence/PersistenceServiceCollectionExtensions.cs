using Microsoft.EntityFrameworkCore;
using Telemetry.BuildingBlocks.Health;

namespace Telemetry.Processor.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public const string ConnectionStringName = "Telemetry";

    /// <summary>
    /// Registers the database context, a readiness check and the start-up migrator.
    /// Must be called before the message consumers are registered so the schema exists before they start.
    /// </summary>
    public static IServiceCollection AddProcessorPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");
        }

        services.AddDbContext<ProcessorDbContext>(options => Configure(options, connectionString));
        services.AddHostedService<DatabaseMigrator>();

        services.AddHealthChecks()
            .AddDbContextCheck<ProcessorDbContext>("postgres", tags: [HealthCheckTags.Ready]);

        return services;
    }

    internal static void Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3))
            .UseSnakeCaseNamingConvention();
}
