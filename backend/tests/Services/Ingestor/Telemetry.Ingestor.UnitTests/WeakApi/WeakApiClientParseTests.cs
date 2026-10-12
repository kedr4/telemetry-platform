using Telemetry.Ingestor.WeakApi;

namespace Telemetry.Ingestor.UnitTests.WeakApi;

public class WeakApiClientParseTests
{
    [Fact]
    public void Parse_MeterArray_ReturnsSuccess()
    {
        var result = WeakApiClient.Parse(
            """[{"type":"energy","name":"Kitchen","payload":{"energy":604.88}},{"type":"motion","name":"Office","payload":{"motionDetected":true}}]""");

        var success = result.ShouldBeOfType<FetchResult.Success>();
        success.Meters.Count.ShouldBe(2);
        success.Meters[0].Type.ShouldBe("energy");
        success.Meters[0].Name.ShouldBe("Kitchen");
    }

    [Fact]
    public void Parse_EmptyArray_ReturnsEmptySuccess()
    {
        WeakApiClient.Parse("[]").ShouldBeOfType<FetchResult.Success>().Meters.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("""{"error": "data corrupted"}""")]
    [InlineData("""{"unexpected": true}""")]
    [InlineData("42")]
    [InlineData("[{\"type\":")]
    [InlineData("Bad Gateway")]
    public void Parse_NonMeterBody_ReturnsCorrupted(string body)
    {
        WeakApiClient.Parse(body).ShouldBeOfType<FetchResult.Corrupted>().Reason.ShouldNotBeNullOrWhiteSpace();
    }
}
