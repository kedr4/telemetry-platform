namespace Telemetry.Gateway.Domain.Aggregations;

/// <summary>What readings are grouped by when aggregating. Dimensions can be combined.</summary>
public enum AggregationDimension
{
    Location = 1,
    Type = 2,
    Time = 3,
}
