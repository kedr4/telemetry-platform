using Telemetry.Gateway.Domain.Aggregations;
using Telemetry.Gateway.Domain.Metrics;

namespace Telemetry.Gateway.Api.GraphQL.Inputs;

/// <summary>What to aggregate and how to group it.</summary>
/// <param name="GroupBy">Dimensions to group by; combine TIME and LOCATION for one series per room.</param>
/// <param name="Metric">Metric to compute statistics of; omit to count readings.</param>
/// <param name="Bucket">Time bucket width; required when grouping by TIME.</param>
/// <param name="Filter">Readings to include.</param>
[GraphQLDescription("What to aggregate and how to group it.")]
public sealed record AggregationInput(
    [property: GraphQLDescription("Dimensions to group by; combine TIME and LOCATION for one series per room.")]
    IReadOnlyList<AggregationDimension> GroupBy,
    [property: GraphQLDescription("Metric to compute statistics of; omit to count readings.")]
    Metric? Metric = null,
    [property: GraphQLDescription("Time bucket width; required when grouping by TIME.")]
    TimeBucket? Bucket = null,
    [property: GraphQLDescription("Readings to include.")]
    ReadingFilterInput? Filter = null);
