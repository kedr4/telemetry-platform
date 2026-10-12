using System.Text.Json;
using Telemetry.Ingestor.WeakApi;

namespace Telemetry.Ingestor.UnitTests.TestData;

/// <summary>Builders for upstream meters, shaped like real WeakApp responses.</summary>
internal static class Meters
{
    public static WeakApiMeterDto Energy(string name = "Kitchen", double energy = 604.88) =>
        Create("energy", name, $$"""{ "energy": {{energy.ToString(System.Globalization.CultureInfo.InvariantCulture)}} }""");

    public static WeakApiMeterDto AirQuality(string name = "Garage", int co2 = 543, int pm25 = 26, int humidity = 68) =>
        Create("air_quality", name, $$"""{ "co2": {{co2}}, "pm25": {{pm25}}, "humidity": {{humidity}} }""");

    public static WeakApiMeterDto Motion(string name = "Office", bool detected = true) =>
        Create("motion", name, $$"""{ "motionDetected": {{(detected ? "true" : "false")}} }""");

    public static WeakApiMeterDto Create(string? type, string? name, string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        return new WeakApiMeterDto(type, name, document.RootElement.Clone());
    }
}
