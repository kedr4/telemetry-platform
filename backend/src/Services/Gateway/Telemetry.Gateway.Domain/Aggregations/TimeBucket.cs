namespace Telemetry.Gateway.Domain.Aggregations;

/// <summary>Width of the time intervals readings are grouped into.</summary>
public enum TimeBucket
{
    Minute = 1,
    Hour = 2,
    Day = 3,
}
