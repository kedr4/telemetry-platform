using Telemetry.Contracts.Readings;
using Telemetry.Notifications.Hubs;

namespace Telemetry.Notifications.UnitTests.Hubs;

public class ReadingNotificationTests
{
    private static readonly DateTimeOffset RecordedAt = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void From_EnergyReading_SetsOnlyEnergy()
    {
        ReadingNotification.From(new EnergyReading("Kitchen", 604.88), RecordedAt)
            .ShouldBe(new ReadingNotification("Kitchen", MeterType.Energy, RecordedAt, Energy: 604.88));
    }

    [Fact]
    public void From_AirQualityReading_SetsAirQualityValues()
    {
        ReadingNotification.From(new AirQualityReading("Garage", 543, 26, 68), RecordedAt)
            .ShouldBe(new ReadingNotification("Garage", MeterType.AirQuality, RecordedAt, Co2: 543, Pm25: 26, Humidity: 68));
    }

    [Fact]
    public void From_MotionReading_SetsOnlyMotion()
    {
        ReadingNotification.From(new MotionReading("Office", MotionDetected: true), RecordedAt)
            .ShouldBe(new ReadingNotification("Office", MeterType.Motion, RecordedAt, MotionDetected: true));
    }
}
