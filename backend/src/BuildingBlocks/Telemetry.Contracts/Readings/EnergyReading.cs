using System.Text.Json.Serialization;

namespace Telemetry.Contracts.Readings;

/// <summary>Energy consumption reading.</summary>
/// <param name="Location">Room the meter is installed in.</param>
/// <param name="Energy">Consumed energy (kWh).</param>
public sealed record EnergyReading(string Location, double Energy) : MeterReading(Location)
{
    [JsonIgnore]
    public override MeterType Type => MeterType.Energy;
}
