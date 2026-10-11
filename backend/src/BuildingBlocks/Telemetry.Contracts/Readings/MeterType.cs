namespace Telemetry.Contracts.Readings;

/// <summary>
/// Kind of smart-home meter reported by the upstream API.
/// </summary>
public enum MeterType
{
    Energy = 1,
    AirQuality = 2,
    Motion = 3,
}
