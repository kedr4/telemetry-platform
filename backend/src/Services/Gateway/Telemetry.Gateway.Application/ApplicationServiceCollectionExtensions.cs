using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Telemetry.Gateway.Application.Behaviors;

namespace Telemetry.Gateway.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayApplication(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceCollectionExtensions).Assembly;

        // Behaviors run in registration order: timing wraps validation, which wraps the handler.
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddOpenBehavior(typeof(QueryLoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Singleton, includeInternalTypes: true);

        return services;
    }
}
