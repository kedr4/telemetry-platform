using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Telemetry.BuildingBlocks.Correlation;
using Telemetry.BuildingBlocks.Health;
using Telemetry.BuildingBlocks.Logging;

namespace Telemetry.BuildingBlocks.Hosting;

/// <summary>
/// Conventions every microservice shares: logging, correlation, error handling and health endpoints.
/// </summary>
public static class ServiceDefaultsExtensions
{
    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddTelemetryLogging();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks();

        return builder;
    }

    public static WebApplication UseServiceDefaults(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging(options => options.GetLevel = GetRequestLogLevel);
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.MapTelemetryHealthChecks();

        return app;
    }

    // Health probes and metric scrapes are frequent and uninteresting unless they fail.
    internal static LogEventLevel GetRequestLogLevel(HttpContext context, double elapsedMs, Exception? exception)
    {
        if (exception is not null || context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            return LogEventLevel.Error;
        }

        var path = context.Request.Path;
        return path.StartsWithSegments("/health") || path.StartsWithSegments("/metrics")
            ? LogEventLevel.Verbose
            : LogEventLevel.Information;
    }
}
