using System.Text.Json.Serialization;
using Telemetry.BuildingBlocks.Messaging;
using Telemetry.Contracts.Messaging;
using Telemetry.Notifications.Diagnostics;
using Telemetry.Notifications.Hubs;
using Telemetry.Notifications.Messaging;

namespace Telemetry.Notifications;

public static class NotificationsExtensions
{
    public const string CorsPolicyName = "frontend";

    public static WebApplicationBuilder AddNotifications(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var services = builder.Services;

        services.AddSignalR(options => options.EnableDetailedErrors = builder.Environment.IsDevelopment())
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        // Browsers connect from the frontend's origin; SignalR needs credentials (cookies/sticky sessions).
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy(CorsPolicyName, policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .WithMethods("GET", "POST")
            .AllowCredentials()));

        services.AddMetrics();
        services.AddSingleton<NotificationsMetrics>();

        // One durable queue shared by all instances: scaling out would need a SignalR backplane
        // (e.g. Redis) or a queue per instance so that every instance sees every notification.
        services.AddMessageConsumer<ReadingsPersisted, ReadingsPersistedHandler>(QueueNames.NotificationsReadingsPersisted);

        return builder;
    }

    public static WebApplication UseNotifications(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseCors(CorsPolicyName);
        app.MapHub<TelemetryHub>(TelemetryHub.Path);

        return app;
    }
}
