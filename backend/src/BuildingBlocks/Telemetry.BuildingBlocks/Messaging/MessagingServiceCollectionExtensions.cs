using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Telemetry.BuildingBlocks.Health;
using Telemetry.Contracts.Messaging;

namespace Telemetry.BuildingBlocks.Messaging;

public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the broker connection (opened at start-up), the message publisher and a readiness check.
    /// Options are bound from the <c>RabbitMq</c> configuration section and validated at start-up.
    /// </summary>
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.AddMetrics();
        services.TryAddSingleton<MessagingMetrics>();
        services.TryAddSingleton<IRabbitMqConnection, RabbitMqConnection>();
        services.TryAddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
        services.AddHostedService<RabbitMqConnectionInitializer>();

        services.AddHealthChecks()
            .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: [HealthCheckTags.Ready]);

        return services;
    }

    /// <summary>
    /// Consumes <paramref name="queueName"/> (declared with a dead-letter queue and bound to the
    /// routing key of <typeparamref name="TMessage"/>) and dispatches messages to <typeparamref name="THandler"/>.
    /// </summary>
    public static IServiceCollection AddMessageConsumer<TMessage, THandler>(this IServiceCollection services, string queueName)
        where TMessage : IIntegrationMessage
        where THandler : class, IMessageHandler<TMessage>
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        services.AddScoped<IMessageHandler<TMessage>, THandler>();
        services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService>(provider =>
            ActivatorUtilities.CreateInstance<RabbitMqConsumer<TMessage>>(provider, queueName));

        return services;
    }
}
