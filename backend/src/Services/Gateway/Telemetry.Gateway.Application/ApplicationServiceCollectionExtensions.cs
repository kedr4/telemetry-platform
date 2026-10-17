using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Telemetry.Gateway.Application.Behaviors;
using Telemetry.Gateway.Application.Diagnostics;

namespace Telemetry.Gateway.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayApplication(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceCollectionExtensions).Assembly;

        services.AddMetrics();
        services.AddSingleton<GatewayMetrics>();

        // Behaviors run in registration order: metrics and timing wrap validation, which wraps the handler.
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddOpenBehavior(typeof(QueryMetricsBehavior<,>));
            configuration.AddOpenBehavior(typeof(QueryLoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Singleton, includeInternalTypes: true);

        return services;
    }
}
