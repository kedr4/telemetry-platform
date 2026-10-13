using Telemetry.Processor.Domain;

namespace Telemetry.Processor.UnitTests.Domain;

public class MeterBatchTests
{
    private static readonly DateTimeOffset FetchedAt = new(2026, 10, 18, 14, 0, 0, TimeSpan.FromHours(2));

    [Fact]
    public void Create_ValidArguments_NormalizesTimestampsToUtc()
    {
        var id = Guid.CreateVersion7();

        var batch = MeterBatch.Create(id, FetchedAt, FetchedAt.AddSeconds(1), readingCount: 18);

        batch.Id.ShouldBe(id);
        batch.FetchedAt.Offset.ShouldBe(TimeSpan.Zero);
        batch.FetchedAt.ShouldBe(FetchedAt);
        batch.ReadingCount.ShouldBe(18);
    }

    [Fact]
    public void Create_EmptyId_Throws()
    {
        Should.Throw<ArgumentException>(() => MeterBatch.Create(Guid.Empty, FetchedAt, FetchedAt, readingCount: 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_NoReadings_Throws(int readingCount)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MeterBatch.Create(Guid.CreateVersion7(), FetchedAt, FetchedAt, readingCount));
    }
}
