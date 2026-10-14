using Telemetry.Gateway.Domain.Meters;

namespace Telemetry.Gateway.Infrastructure.Persistence;

/// <summary>Maps meter types to and from the wire names stored in <c>meters.type</c>.</summary>
internal static class MeterTypeColumn
{
    public static string ToColumn(MeterType type) => type switch
    {
        MeterType.Energy => "energy",
        MeterType.AirQuality => "air_quality",
        MeterType.Motion => "motion",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown meter type."),
    };

    public static MeterType FromColumn(string value) => value switch
    {
        "energy" => MeterType.Energy,
        "air_quality" => MeterType.AirQuality,
        "motion" => MeterType.Motion,
        _ => throw new InvalidOperationException($"Unknown meter type '{value}' in the database."),
    };
}
