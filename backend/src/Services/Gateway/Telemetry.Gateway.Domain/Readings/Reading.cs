using Telemetry.Gateway.Domain.Meters;

namespace Telemetry.Gateway.Domain.Readings;

/// <summary>A measurement taken by a meter. One derived type per <see cref="MeterType"/>.</summary>
/// <param name="Id">Database identifier; together with <paramref name="RecordedAt"/> it orders readings.</param>
/// <param name="MeterId">Identifier of the meter that took the reading.</param>
/// <param name="Location">Room the meter is installed in.</param>
/// <param name="RecordedAt">When the reading was taken.</param>
public abstract record Reading(long Id, int MeterId, string Location, DateTimeOffset RecordedAt)
{
    public abstract MeterType Type { get; }
}
