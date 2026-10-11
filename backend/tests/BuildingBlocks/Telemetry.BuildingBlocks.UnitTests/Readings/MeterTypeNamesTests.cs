using Telemetry.Contracts.Readings;

namespace Telemetry.BuildingBlocks.UnitTests.Readings;

public class MeterTypeNamesTests
{
    [Theory]
    [InlineData("energy", MeterType.Energy)]
    [InlineData("air_quality", MeterType.AirQuality)]
    [InlineData("motion", MeterType.Motion)]
    public void TryParse_KnownWireName_ReturnsMeterType(string wireName, MeterType expected)
    {
        MeterTypeNames.TryParse(wireName, out var type).ShouldBeTrue();
        type.ShouldBe(expected);
        type.ToWireName().ShouldBe(wireName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Energy")]
    [InlineData("temperature")]
    public void TryParse_UnknownWireName_ReturnsFalse(string? wireName)
    {
        MeterTypeNames.TryParse(wireName, out _).ShouldBeFalse();
    }
}
