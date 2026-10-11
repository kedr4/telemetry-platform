using System.Text.Json.Serialization;

namespace Telemetry.Contracts.Readings;

/// <summary>Air quality reading.</summary>
/// <param name="Location">Room the meter is installed in.</param>
/// <param name="Co2">CO2 concentration (ppm).</param>
/// <param name="Pm25">Fine particulate matter PM2.5 (µg/m³).</param>
/// <param name="Humidity">Relative humidity (%).</param>
public sealed record AirQualityReading(string Location, int Co2, int Pm25, int Humidity) : MeterReading(Location)
{
    [JsonIgnore]
    public override MeterType Type => MeterType.AirQuality;
}
