namespace Telemetry.Gateway.Domain.Metrics;

/// <summary>A numeric quantity that can be aggregated.</summary>
public enum Metric
{
    /// <summary>Energy consumption (kWh).</summary>
    Energy = 1,

    /// <summary>CO2 concentration (ppm).</summary>
    Co2 = 2,

    /// <summary>PM2.5 concentration (µg/m³).</summary>
    Pm25 = 3,

    /// <summary>Relative humidity (%).</summary>
    Humidity = 4,

    /// <summary>Motion detected, as 1 (yes) or 0 (no): its average is the share of readings with motion.</summary>
    Motion = 5,
}
