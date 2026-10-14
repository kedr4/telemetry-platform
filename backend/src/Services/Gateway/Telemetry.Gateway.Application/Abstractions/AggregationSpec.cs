using Telemetry.Gateway.Application.Common;
using Telemetry.Gateway.Domain.Aggregations;
using Telemetry.Gateway.Domain.Metrics;

namespace Telemetry.Gateway.Application.Abstractions;

/// <summary>A validated aggregation request.</summary>
/// <param name="GroupBy">Dimensions to group by (distinct, non-empty).</param>
/// <param name="Metric">Metric to compute statistics of; <c>null</c> to only count readings.</param>
/// <param name="Bucket">Time bucket width; set exactly when grouping by time.</param>
/// <param name="Filter">Readings to include.</param>
public sealed record AggregationSpec(
    IReadOnlyList<AggregationDimension> GroupBy,
    Metric? Metric,
    TimeBucket? Bucket,
    ReadingFilter Filter);
