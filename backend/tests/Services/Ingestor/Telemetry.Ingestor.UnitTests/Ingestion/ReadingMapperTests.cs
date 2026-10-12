using Telemetry.Contracts.Readings;
using Telemetry.Ingestor.Ingestion;
using Telemetry.Ingestor.UnitTests.TestData;

namespace Telemetry.Ingestor.UnitTests.Ingestion;

public class ReadingMapperTests
{
    [Fact]
    public void TryMap_EnergyMeter_ReturnsEnergyReading()
    {
        ReadingMapper.TryMap(Meters.Energy("Kitchen", 604.88), out var reading, out _).ShouldBeTrue();

        reading.ShouldBe(new EnergyReading("Kitchen", 604.88));
    }

    [Fact]
    public void TryMap_AirQualityMeter_ReturnsAirQualityReading()
    {
        ReadingMapper.TryMap(Meters.AirQuality("Garage", 543, 26, 68), out var reading, out _).ShouldBeTrue();

        reading.ShouldBe(new AirQualityReading("Garage", 543, 26, 68));
    }

    [Fact]
    public void TryMap_MotionMeter_ReturnsMotionReading()
    {
        ReadingMapper.TryMap(Meters.Motion("Office", detected: false), out var reading, out _).ShouldBeTrue();

        reading.ShouldBe(new MotionReading("Office", MotionDetected: false));
    }

    [Fact]
    public void TryMap_TrimsLocation()
    {
        ReadingMapper.TryMap(Meters.Motion("  Living Room "), out var reading, out _).ShouldBeTrue();

        reading.Location.ShouldBe("Living Room");
    }

    [Theory]
    [InlineData("temperature", "Kitchen", """{ "celsius": 21 }""")]
    [InlineData(null, "Kitchen", """{ "energy": 1 }""")]
    [InlineData("energy", null, """{ "energy": 1 }""")]
    [InlineData("energy", "  ", """{ "energy": 1 }""")]
    [InlineData("energy", "Kitchen", """null""")]
    [InlineData("energy", "Kitchen", """[1, 2]""")]
    [InlineData("energy", "Kitchen", """{ }""")]
    [InlineData("energy", "Kitchen", """{ "energy": "604.88" }""")]
    [InlineData("air_quality", "Garage", """{ "co2": 543, "pm25": 26 }""")]
    [InlineData("air_quality", "Garage", """{ "co2": 543.5, "pm25": 26, "humidity": 68 }""")]
    [InlineData("motion", "Office", """{ "motionDetected": "yes" }""")]
    [InlineData("motion", "Office", """{ "motionDetected": null }""")]
    public void TryMap_MalformedMeter_ReturnsError(string? type, string? name, string payload)
    {
        ReadingMapper.TryMap(Meters.Create(type, name, payload), out var reading, out var error).ShouldBeFalse();

        reading.ShouldBeNull();
        error.ShouldNotBeNullOrWhiteSpace();
    }
}
