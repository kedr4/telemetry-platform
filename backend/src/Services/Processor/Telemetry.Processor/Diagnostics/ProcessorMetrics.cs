using System.Diagnostics.Metrics;

namespace Telemetry.Processor.Diagnostics;

/// <summary>Persistence metrics, exported through OpenTelemetry.</summary>
public sealed class ProcessorMetrics
{
    public const string MeterName = "Telemetry.Processor";

    private readonly Counter<long> _batches;
    private readonly Counter<long> _readings;
    private readonly Histogram<double> _duration;

    public ProcessorMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        var meter = meterFactory.Create(MeterName);
        _batches = meter.CreateCounter<long>(
            "telemetry.processor.batches",
            unit: "{batch}",
            description: "Batches handled, by outcome (stored or duplicate).");
        _readings = meter.CreateCounter<long>(
            "telemetry.processor.readings.stored",
            unit: "{reading}",
            description: "Readings written to the database.");
        _duration = meter.CreateHistogram<double>(
            "telemetry.processor.batch.duration",
            unit: "ms",
            description: "Time to store one batch.");
    }

    public void RecordBatch(bool stored, int readings, TimeSpan duration)
    {
        _batches.Add(1, new KeyValuePair<string, object?>("outcome", stored ? "stored" : "duplicate"));
        _readings.Add(readings);
        _duration.Record(duration.TotalMilliseconds);
    }
}
