using Telemetry.Gateway.Domain.Meters;

namespace Telemetry.Gateway.Domain.Readings;

/// <summary>Motion sensor reading.</summary>
/// <param name="Id">Database identifier.</param>
/// <param name="MeterId">Identifier of the meter.</param>
/// <param name="Location">Room the meter is installed in.</param>
/// <param name="RecordedAt">When the reading was taken.</param>
/// <param name="MotionDetected">Whether motion was detected.</param>
public sealed record MotionReading(long Id, int MeterId, string Location, DateTimeOffset RecordedAt, bool MotionDetected)
    : Reading(Id, MeterId, Location, RecordedAt)
{
    public override MeterType Type => MeterType.Motion;
}
