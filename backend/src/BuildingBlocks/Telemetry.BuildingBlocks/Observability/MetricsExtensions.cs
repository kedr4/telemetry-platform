using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Telemetry.BuildingBlocks.Observability;

/// <summary>
/// Exports metrics in the Prometheus text format on <c>/metrics</c>: the services' own meters plus the
/// meters built into ASP.NET Core, SignalR, HttpClient, the .NET runtime, Polly and Npgsql.
/// </summary>
public static class MetricsExtensions
{
    public const string ScrapePath = "/metrics";

    /// <summary>Every meter of this solution is named <c>Telemetry.*</c>.</summary>
    internal const string ServiceMeters = "Telemetry.*";

    internal static readonly string[] PlatformMeters =
    [
        "Microsoft.AspNetCore.Hosting",
        "Microsoft.AspNetCore.Server.Kestrel",
        "Microsoft.AspNetCore.Http.Connections",
        "Microsoft.AspNetCore.Diagnostics",
        "System.Net.Http",
        "System.Runtime",
        "Polly",
        "Npgsql",
    ];

    public static WebApplicationBuilder AddTelemetryMetrics(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
            .WithMetrics(metrics => metrics
                .AddMeter(ServiceMeters)
                .AddMeter(PlatformMeters)
                .AddPrometheusExporter());

        return builder;
    }

    public static IEndpointRouteBuilder MapTelemetryMetrics(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPrometheusScrapingEndpoint(ScrapePath);
        return endpoints;
    }
}
