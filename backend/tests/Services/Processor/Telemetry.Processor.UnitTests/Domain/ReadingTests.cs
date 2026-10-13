using Telemetry.Contracts.Readings;
using Telemetry.Processor.Domain;

namespace Telemetry.Processor.UnitTests.Domain;

public class ReadingTests
{
    private static readonly Guid BatchId = Guid.CreateVersion7();
    private static readonly DateTimeOffset RecordedAt = new(2026, 10, 18, 14, 0, 0, TimeSpan.FromHours(2));

    [Fact]
    public void Record_EnergyReading_SetsOnlyEnergy()
    {
        var reading = Reading.Record(Meter.Create("Kitchen", MeterType.Energy), BatchId, RecordedAt, new EnergyReading("Kitchen", 604.88));

        reading.Energy.ShouldBe(604.88);
        reading.Co2.ShouldBeNull();
        reading.Pm25.ShouldBeNull();
        reading.Humidity.ShouldBeNull();
        reading.MotionDetected.ShouldBeNull();
    }

    [Fact]
    public void Record_AirQualityReading_SetsAirQualityValues()
    {
        var reading = Reading.Record(
            Meter.Create("Garage", MeterType.AirQuality),
            BatchId,
            RecordedAt,
            new AirQualityReading("Garage", 543, 26, 68));

        (reading.Co2, reading.Pm25, reading.Humidity).ShouldBe((543, 26, 68));
        reading.Energy.ShouldBeNull();
        reading.MotionDetected.ShouldBeNull();
    }

    [Fact]
    public void Record_MotionReading_SetsOnlyMotion()
    {
        var reading = Reading.Record(Meter.Create("Office", MeterType.Motion), BatchId, RecordedAt, new MotionReading("Office", MotionDetected: false));

        reading.MotionDetected.ShouldBe(false);
        reading.Energy.ShouldBeNull();
        reading.Co2.ShouldBeNull();
    }

    [Fact]
    public void Record_StoresBatchAndUtcTimestamp()
    {
        var reading = Reading.Record(Meter.Create("Office", MeterType.Motion), BatchId, RecordedAt, new MotionReading("Office", MotionDetected: true));

        reading.BatchId.ShouldBe(BatchId);
        reading.RecordedAt.Offset.ShouldBe(TimeSpan.Zero);
        reading.RecordedAt.ShouldBe(RecordedAt);
    }

    [Fact]
    public void Record_ReadingOfAnotherType_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            Reading.Record(Meter.Create("Kitchen", MeterType.Energy), BatchId, RecordedAt, new MotionReading("Kitchen", MotionDetected: true)));
    }

    [Fact]
    public void Record_ReadingFromAnotherLocation_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            Reading.Record(Meter.Create("Kitchen", MeterType.Energy), BatchId, RecordedAt, new EnergyReading("Garage", 1)));
    }
}
