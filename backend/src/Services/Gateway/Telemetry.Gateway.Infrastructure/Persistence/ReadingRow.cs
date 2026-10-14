namespace Telemetry.Gateway.Infrastructure.Persistence;

/// <summary>Row of the <c>readings</c> table (schema owned by the Processor). Only the columns the gateway reads.</summary>
internal sealed class ReadingRow
{
    public long Id { get; set; }

    public int MeterId { get; set; }

    public MeterRow Meter { get; set; } = null!;

    public DateTimeOffset RecordedAt { get; set; }

    public double? Energy { get; set; }

    public int? Co2 { get; set; }

    public int? Pm25 { get; set; }

    public int? Humidity { get; set; }

    public bool? MotionDetected { get; set; }
}
