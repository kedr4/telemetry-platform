using Telemetry.Gateway.Domain.Meters;
using Telemetry.Gateway.Domain.Metrics;

namespace Telemetry.Gateway.UnitTests.Domain;

public class MetricExtensionsTests
{
    [Theory]
    [InlineData(Metric.Energy, MeterType.Energy)]
    [InlineData(Metric.Co2, MeterType.AirQuality)]
    [InlineData(Metric.Pm25, MeterType.AirQuality)]
    [InlineData(Metric.Humidity, MeterType.AirQuality)]
    [InlineData(Metric.Motion, MeterType.Motion)]
    public void MeasuredBy_ReturnsMeterTypeOfMetric(Metric metric, MeterType expected)
    {
        metric.MeasuredBy().ShouldBe(expected);
    }

    [Fact]
    public void MeasuredBy_UndefinedMetric_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ((Metric)42).MeasuredBy());
    }
}
