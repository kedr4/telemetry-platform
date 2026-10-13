using Telemetry.Contracts.Readings;

namespace Telemetry.Processor.Domain;

/// <summary>
/// A stored measurement. Values are kept in typed columns (one group per meter type) so that they can be
/// filtered and aggregated efficiently in SQL; exactly one group is set, which the database also enforces.
/// </summary>
public sealed class Reading
{
    private Reading(Meter meter, Guid batchId, DateTimeOffset recordedAt)
    {
        Meter = meter;
        BatchId = batchId;
        RecordedAt = recordedAt;
    }

    // Used by EF Core when materializing.
    private Reading()
    {
        Meter = null!;
    }

    // Get-only: assigned by the database and written by EF Core through the backing fields.
    public long Id { get; }

    public int MeterId { get; }

    public Meter Meter { get; private set; }

    public Guid BatchId { get; private set; }

    public DateTimeOffset RecordedAt { get; private set; }

    public double? Energy { get; private set; }

    public int? Co2 { get; private set; }

    public int? Pm25 { get; private set; }

    public int? Humidity { get; private set; }

    public bool? MotionDetected { get; private set; }

    public static Reading Record(Meter meter, Guid batchId, DateTimeOffset recordedAt, MeterReading value)
    {
        ArgumentNullException.ThrowIfNull(meter);
        ArgumentNullException.ThrowIfNull(value);

        if (value.Type != meter.Type || value.Location != meter.Location)
        {
            throw new ArgumentException(
                $"A '{value.Type}' reading from '{value.Location}' cannot be recorded for the '{meter.Type}' meter in '{meter.Location}'.",
                nameof(value));
        }

        var reading = new Reading(meter, batchId, recordedAt.ToUniversalTime());

        switch (value)
        {
            case EnergyReading energy:
                reading.Energy = energy.Energy;
                break;
            case AirQualityReading airQuality:
                reading.Co2 = airQuality.Co2;
                reading.Pm25 = airQuality.Pm25;
                reading.Humidity = airQuality.Humidity;
                break;
            case MotionReading motion:
                reading.MotionDetected = motion.MotionDetected;
                break;
            default:
                throw new ArgumentException($"Unsupported reading type {value.GetType().Name}.", nameof(value));
        }

        return reading;
    }
}
