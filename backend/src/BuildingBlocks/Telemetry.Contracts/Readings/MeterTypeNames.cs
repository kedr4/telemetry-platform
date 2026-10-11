namespace Telemetry.Contracts.Readings;

/// <summary>
/// Wire names of <see cref="MeterType"/> values, as used by the upstream API and in messages.
/// </summary>
public static class MeterTypeNames
{
    public const string Energy = "energy";
    public const string AirQuality = "air_quality";
    public const string Motion = "motion";

    public static string ToWireName(this MeterType type) => type switch
    {
        MeterType.Energy => Energy,
        MeterType.AirQuality => AirQuality,
        MeterType.Motion => Motion,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown meter type."),
    };

    public static bool TryParse(string? wireName, out MeterType type)
    {
        switch (wireName)
        {
            case Energy:
                type = MeterType.Energy;
                return true;
            case AirQuality:
                type = MeterType.AirQuality;
                return true;
            case Motion:
                type = MeterType.Motion;
                return true;
            default:
                type = default;
                return false;
        }
    }
}
