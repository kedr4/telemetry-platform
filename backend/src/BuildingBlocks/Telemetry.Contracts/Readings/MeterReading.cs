using System.Text.Json.Serialization;

namespace Telemetry.Contracts.Readings;

/// <summary>
/// A single measurement taken by a meter at a location. Serialized polymorphically using the
/// same <c>type</c> discriminator values as the upstream API.
/// </summary>
/// <param name="Location">Room the meter is installed in, e.g. <c>Kitchen</c>.</param>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(EnergyReading), MeterTypeNames.Energy)]
[JsonDerivedType(typeof(AirQualityReading), MeterTypeNames.AirQuality)]
[JsonDerivedType(typeof(MotionReading), MeterTypeNames.Motion)]
public abstract record MeterReading(string Location)
{
    [JsonIgnore]
    public abstract MeterType Type { get; }
}
