using FluentValidation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Telemetry.BuildingBlocks.Health;
using Telemetry.Ingestor.Diagnostics;
using Telemetry.Ingestor.Ingestion;
using Telemetry.Ingestor.WeakApi;

namespace Telemetry.Ingestor;

public static class IngestorServiceCollectionExtensions
{
    public static IServiceCollection AddIngestor(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<IngestionOptions>()
            .Bind(configuration.GetSection(IngestionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddWeakApiClient(configuration);
        services.AddValidatorsFromAssemblyContaining<IngestionWorker>(ServiceLifetime.Singleton, includeInternalTypes: true);

        services.AddMetrics();
        services.AddSingleton<IngestorMetrics>();
        services.AddSingleton<IngestionStatus>();
        services.AddSingleton<BatchIngestionService>();
        services.AddHostedService<IngestionWorker>();

        services.AddHealthChecks()
            .AddCheck<IngestionHealthCheck>("ingestion", tags: [HealthCheckTags.Ready]);

        return services;
    }

    /// <summary>
    /// Registers the typed upstream client with its resilience pipeline. Public so it can be exercised in isolation.
    /// </summary>
    public static IServiceCollection AddWeakApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<WeakApiOptions>()
            .Bind(configuration.GetSection(WeakApiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.AddTransient<ResponseBufferingHandler>();
        services.AddSingleton<WeakApiRateLimiter>();

        var httpClient = services.AddHttpClient<IWeakApiClient, WeakApiClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<WeakApiOptions>>().Value;
                client.BaseAddress = options.BaseUrl;
                client.DefaultRequestHeaders.Add(WeakApiClient.ApiKeyHeader, options.ApiKey);

                // Timeouts are enforced by the resilience pipeline; this is only a safety net.
                client.Timeout = options.TotalTimeout + TimeSpan.FromSeconds(5);
            });

        httpClient.AddResilienceHandler(WeakApiResilience.PipelineName, (pipeline, context) =>
            {
                var options = context.ServiceProvider.GetRequiredService<IOptions<WeakApiOptions>>().Value;
                var logger = context.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<WeakApiClient>();
                var rateLimiter = context.ServiceProvider.GetRequiredService<WeakApiRateLimiter>().Limiter;
                WeakApiResilience.Configure(pipeline, options, rateLimiter, logger);
            });

        // Added after the resilience handler, so it runs inside every attempt.
        httpClient.AddHttpMessageHandler<ResponseBufferingHandler>();

        return services;
    }
}
