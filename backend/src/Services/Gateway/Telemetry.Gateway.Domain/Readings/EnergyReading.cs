using Telemetry.Gateway.Domain.Meters;

namespace Telemetry.Gateway.Domain.Readings;

/// <summary>Energy consumption reading.</summary>
/// <param name="Id">Database identifier.</param>
/// <param name="MeterId">Identifier of the meter.</param>
/// <param name="Location">Room the meter is installed in.</param>
/// <param name="RecordedAt">When the reading was taken.</param>
/// <param name="Energy">Consumed energy (kWh).</param>
public sealed record EnergyReading(long Id, int MeterId, string Location, DateTimeOffset RecordedAt, double Energy)
    : Reading(Id, MeterId, Location, RecordedAt)
{
    public override MeterType Type => MeterType.Energy;
}
