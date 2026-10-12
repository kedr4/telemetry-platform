using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Telemetry.BuildingBlocks.Logging;

public static class LoggingExtensions
{
    /// <summary>Configuration key selecting the console format: <c>json</c> or <c>text</c>.</summary>
    public const string LogFormatKey = "LogFormat";

    private const string TextTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}";

    /// <summary>
    /// Replaces the default logging providers with Serilog: structured, enriched with service and
    /// correlation data, JSON in containers and human-readable text in development.
    /// Levels and sinks can be overridden through the standard <c>Serilog</c> configuration section.
    /// </summary>
    public static IHostApplicationBuilder AddTelemetryLogging(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var defaultFormat = builder.Environment.IsDevelopment() ? "text" : "json";
        var useJson = string.Equals(
            builder.Configuration[LogFormatKey] ?? defaultFormat,
            "json",
            StringComparison.OrdinalIgnoreCase);

        // Polly's built-in telemetry logs every attempt, so it is limited to errors;
        // services log the resilience events that matter themselves.
        builder.Services.AddSerilog((services, logger) =>
        {
            logger
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                .MinimumLevel.Override("Polly", LogEventLevel.Error)
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("Service", builder.Environment.ApplicationName);

            if (useJson)
            {
                logger.WriteTo.Console(new RenderedCompactJsonFormatter());
            }
            else
            {
                logger.WriteTo.Console(outputTemplate: TextTemplate, formatProvider: System.Globalization.CultureInfo.InvariantCulture);
            }
        });

        return builder;
    }
}
