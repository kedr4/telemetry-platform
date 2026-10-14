using Telemetry.Gateway.Domain.Meters;

namespace Telemetry.Gateway.Domain.Readings;

/// <summary>Air quality reading.</summary>
/// <param name="Id">Database identifier.</param>
/// <param name="MeterId">Identifier of the meter.</param>
/// <param name="Location">Room the meter is installed in.</param>
/// <param name="RecordedAt">When the reading was taken.</param>
/// <param name="Co2">CO2 concentration (ppm).</param>
/// <param name="Pm25">Fine particulate matter PM2.5 (µg/m³).</param>
/// <param name="Humidity">Relative humidity (%).</param>
public sealed record AirQualityReading(
    long Id,
    int MeterId,
    string Location,
    DateTimeOffset RecordedAt,
    int Co2,
    int Pm25,
    int Humidity) : Reading(Id, MeterId, Location, RecordedAt)
{
    public override MeterType Type => MeterType.AirQuality;
}
