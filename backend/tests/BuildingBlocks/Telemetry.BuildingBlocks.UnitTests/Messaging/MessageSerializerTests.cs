using System.Text;
using System.Text.Json;
using Telemetry.BuildingBlocks.Messaging;
using Telemetry.Contracts.Messaging;
using Telemetry.Contracts.Readings;

namespace Telemetry.BuildingBlocks.UnitTests.Messaging;

public class MessageSerializerTests
{
    private static readonly MeterBatchFetched Batch = new(
        Guid.Parse("8f0f5a52-3c1e-4f43-9a8e-0b9f5d1c2e11"),
        new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero),
        [
            new EnergyReading("Kitchen", 604.88),
            new AirQualityReading("Garage", 543, 26, 68),
            new MotionReading("Office", MotionDetected: true),
        ]);

    [Fact]
    public void Serialize_UsesUpstreamTypeDiscriminators()
    {
        var json = Encoding.UTF8.GetString(MessageSerializer.Serialize(Batch));

        json.ShouldContain("\"type\":\"energy\"");
        json.ShouldContain("\"type\":\"air_quality\"");
        json.ShouldContain("\"type\":\"motion\"");
    }

    [Fact]
    public void RoundTrip_PreservesDerivedReadingTypesAndValues()
    {
        var body = MessageSerializer.Serialize(Batch);

        var result = MessageSerializer.Deserialize<MeterBatchFetched>(body);

        result.BatchId.ShouldBe(Batch.BatchId);
        result.FetchedAt.ShouldBe(Batch.FetchedAt);
        result.Readings.ShouldBe(Batch.Readings);
        result.Readings.Select(r => r.Type).ShouldBe([MeterType.Energy, MeterType.AirQuality, MeterType.Motion]);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("{\"batchId\":\"8f0f5a52-3c1e-4f43-9a8e-0b9f5d1c2e11\"}")]
    [InlineData("{\"batchId\":\"8f0f5a52-3c1e-4f43-9a8e-0b9f5d1c2e11\",\"fetchedAt\":\"2026-10-01T12:00:00Z\",\"readings\":[{\"type\":\"unknown\",\"location\":\"Kitchen\"}]}")]
    public void Deserialize_MalformedBody_ThrowsJsonException(string body)
    {
        Should.Throw<JsonException>(() => MessageSerializer.Deserialize<MeterBatchFetched>(Encoding.UTF8.GetBytes(body)));
    }
}
