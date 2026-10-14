using MediatR;
using Telemetry.Gateway.Application.Common;
using Telemetry.Gateway.Domain.Aggregations;
using Telemetry.Gateway.Domain.Metrics;

namespace Telemetry.Gateway.Application.Aggregations.GetAggregates;

/// <summary>
/// Groups readings by location, type and/or time bucket. With a metric, statistics of that metric are
/// computed (only readings of the meter type measuring it are considered); without one, readings are counted.
/// </summary>
/// <param name="GroupBy">Dimensions to group by; can be combined (e.g. time and location for one series per room).</param>
/// <param name="Metric">Metric to compute statistics of, or <c>null</c> to count readings.</param>
/// <param name="Bucket">Time bucket width; required exactly when grouping by time.</param>
/// <param name="Filter">Readings to include.</param>
public sealed record GetAggregatesQuery(
    IReadOnlyList<AggregationDimension> GroupBy,
    Metric? Metric,
    TimeBucket? Bucket,
    ReadingFilter Filter) : IRequest<IReadOnlyList<AggregateResult>>;
