using Telemetry.Gateway.Domain.Meters;

namespace Telemetry.Gateway.Domain.Aggregations;

/// <summary>
/// Statistics of one group. Grouping keys that were not requested are <c>null</c>; so are the value
/// statistics when no metric was requested (only <see cref="Count"/> is computed then).
/// </summary>
/// <param name="Location">Location of the group, when grouped by location.</param>
/// <param name="Type">Meter type of the group, when grouped by type.</param>
/// <param name="BucketStart">Start of the time bucket, when grouped by time.</param>
/// <param name="Count">Number of readings in the group.</param>
/// <param name="Average">Average of the metric.</param>
/// <param name="Minimum">Minimum of the metric.</param>
/// <param name="Maximum">Maximum of the metric.</param>
/// <param name="Sum">Sum of the metric.</param>
public sealed record AggregateResult(
    string? Location,
    MeterType? Type,
    DateTimeOffset? BucketStart,
    long Count,
    double? Average,
    double? Minimum,
    double? Maximum,
    double? Sum);
