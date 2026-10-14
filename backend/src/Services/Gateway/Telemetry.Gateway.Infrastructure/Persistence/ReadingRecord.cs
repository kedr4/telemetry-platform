using Telemetry.Gateway.Domain.Readings;

namespace Telemetry.Gateway.Infrastructure.Persistence;

/// <summary>A reading joined with its meter, as returned by queries; converted to the domain model.</summary>
internal sealed record ReadingRecord(
    long Id,
    int MeterId,
    string Location,
    string Type,
    DateTimeOffset RecordedAt,
    double? Energy,
    int? Co2,
    int? Pm25,
    int? Humidity,
    bool? MotionDetected)
{
    public Reading ToDomain() => MeterTypeColumn.FromColumn(Type) switch
    {
        Domain.Meters.MeterType.Energy => new EnergyReading(Id, MeterId, Location, RecordedAt, Required(Energy)),
        Domain.Meters.MeterType.AirQuality => new AirQualityReading(
            Id, MeterId, Location, RecordedAt, Required(Co2), Required(Pm25), Required(Humidity)),
        Domain.Meters.MeterType.Motion => new MotionReading(Id, MeterId, Location, RecordedAt, Required(MotionDetected)),
        _ => throw new InvalidOperationException($"Unsupported meter type '{Type}'."),
    };

    // The schema's check constraints guarantee the columns of the reading's type are set.
    private T Required<T>(T? value)
        where T : struct =>
        value ?? throw new InvalidOperationException($"Reading {Id} of type '{Type}' is missing a value.");
}
