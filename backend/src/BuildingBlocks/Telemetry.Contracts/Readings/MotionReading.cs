using System.Text.Json.Serialization;

namespace Telemetry.Contracts.Readings;

/// <summary>Motion sensor reading.</summary>
/// <param name="Location">Room the meter is installed in.</param>
/// <param name="MotionDetected">Whether motion was detected.</param>
public sealed record MotionReading(string Location, bool MotionDetected) : MeterReading(Location)
{
    [JsonIgnore]
    public override MeterType Type => MeterType.Motion;
}
