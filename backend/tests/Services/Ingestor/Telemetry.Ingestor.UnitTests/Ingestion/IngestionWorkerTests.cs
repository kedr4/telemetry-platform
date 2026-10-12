using Telemetry.Ingestor.Ingestion;

namespace Telemetry.Ingestor.UnitTests.Ingestion;

public class IngestionWorkerTests
{
    private static readonly IngestionOptions Options = new()
    {
        PollInterval = TimeSpan.FromSeconds(5),
        MaxBackoff = TimeSpan.FromSeconds(60),
    };

    [Theory]
    [InlineData(null, 5)]
    [InlineData(2, 5)]
    [InlineData(23, 23)]
    [InlineData(600, 60)]
    public void GetDelay_HonoursRetryAfterWithinBounds(int? retryAfterSeconds, int expectedSeconds)
    {
        var result = new IngestionResult(
            IngestionOutcome.RateLimited,
            RetryAfter: retryAfterSeconds is { } seconds ? TimeSpan.FromSeconds(seconds) : null);

        IngestionWorker.GetDelay(result, Options).ShouldBe(TimeSpan.FromSeconds(expectedSeconds));
    }
}
