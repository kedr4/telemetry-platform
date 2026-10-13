using Telemetry.Contracts.Readings;
using Telemetry.Processor.Domain;

namespace Telemetry.Processor.UnitTests.Domain;

public class MeterTests
{
    [Fact]
    public void Create_ValidArguments_CreatesMeter()
    {
        var meter = Meter.Create("Living Room", MeterType.AirQuality);

        meter.Location.ShouldBe("Living Room");
        meter.Type.ShouldBe(MeterType.AirQuality);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" Kitchen")]
    [InlineData("Kitchen ")]
    public void Create_InvalidLocation_Throws(string location)
    {
        Should.Throw<ArgumentException>(() => Meter.Create(location, MeterType.Energy));
    }

    [Fact]
    public void Create_TooLongLocation_Throws()
    {
        Should.Throw<ArgumentException>(() => Meter.Create(new string('x', Meter.MaxLocationLength + 1), MeterType.Energy));
    }

    [Fact]
    public void Create_UnknownType_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Meter.Create("Kitchen", (MeterType)42));
    }
}
