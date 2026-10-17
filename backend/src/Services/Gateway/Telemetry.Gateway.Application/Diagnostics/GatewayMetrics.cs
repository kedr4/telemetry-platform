using System.Diagnostics.Metrics;

namespace Telemetry.Gateway.Application.Diagnostics;

/// <summary>Query metrics, exported through OpenTelemetry. Shared by the GraphQL and REST APIs.</summary>
public sealed class GatewayMetrics
{
    public const string MeterName = "Telemetry.Gateway";

    private readonly Histogram<double> _queryDuration;

    public GatewayMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        var meter = meterFactory.Create(MeterName);
        _queryDuration = meter.CreateHistogram<double>(
            "telemetry.gateway.query.duration",
            unit: "ms",
            description: "Time to execute one query, by query and outcome (success, invalid or error).");
    }

    public void RecordQuery(string query, string outcome, TimeSpan duration) =>
        _queryDuration.Record(
            duration.TotalMilliseconds,
            new KeyValuePair<string, object?>("query", query),
            new KeyValuePair<string, object?>("outcome", outcome));
}
