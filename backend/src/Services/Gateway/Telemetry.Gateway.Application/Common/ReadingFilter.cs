using Telemetry.Gateway.Domain.Meters;

namespace Telemetry.Gateway.Application.Common;

/// <summary>Restricts readings to locations, meter types and a time range. Empty or null means "no restriction".</summary>
/// <param name="Locations">Locations to include.</param>
/// <param name="Types">Meter types to include.</param>
/// <param name="From">Inclusive lower bound of <c>RecordedAt</c>.</param>
/// <param name="To">Exclusive upper bound of <c>RecordedAt</c>.</param>
public sealed record ReadingFilter(
    IReadOnlyCollection<string> Locations,
    IReadOnlyCollection<MeterType> Types,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null)
{
    public static ReadingFilter None { get; } = new([], []);
}
