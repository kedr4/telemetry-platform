using Telemetry.Gateway.Domain.Meters;

namespace Telemetry.Gateway.Domain.Metrics;

public static class MetricExtensions
{
    /// <summary>The type of meter that measures the metric.</summary>
    public static MeterType MeasuredBy(this Metric metric) => metric switch
    {
        Metric.Energy => MeterType.Energy,
        Metric.Co2 or Metric.Pm25 or Metric.Humidity => MeterType.AirQuality,
        Metric.Motion => MeterType.Motion,
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, "Unknown metric."),
    };
}
