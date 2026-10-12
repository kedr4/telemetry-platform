using System.Diagnostics.Metrics;
using Telemetry.Ingestor.Ingestion;

namespace Telemetry.Ingestor.Diagnostics;

/// <summary>Ingestion metrics, exported through OpenTelemetry.</summary>
public sealed class IngestorMetrics
{
    public const string MeterName = "Telemetry.Ingestor";

    private readonly Counter<long> _cycles;
    private readonly Counter<long> _publishedReadings;
    private readonly Counter<long> _rejectedReadings;

    public IngestorMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        var meter = meterFactory.Create(MeterName);
        _cycles = meter.CreateCounter<long>(
            "telemetry.ingestor.cycles",
            unit: "{cycle}",
            description: "Ingestion cycles by outcome.");
        _publishedReadings = meter.CreateCounter<long>(
            "telemetry.ingestor.readings.published",
            unit: "{reading}",
            description: "Readings published to the broker.");
        _rejectedReadings = meter.CreateCounter<long>(
            "telemetry.ingestor.readings.rejected",
            unit: "{reading}",
            description: "Readings dropped because they were malformed, invalid or duplicated.");
    }

    public void Record(IngestionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        _cycles.Add(1, new KeyValuePair<string, object?>("outcome", result.Outcome.ToString()));
        _publishedReadings.Add(result.PublishedReadings);
        _rejectedReadings.Add(result.RejectedReadings);
    }
}
