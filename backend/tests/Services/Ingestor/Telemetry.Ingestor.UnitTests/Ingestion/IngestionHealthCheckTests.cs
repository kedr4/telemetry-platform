using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Time.Testing;
using Telemetry.Ingestor.Ingestion;

namespace Telemetry.Ingestor.UnitTests.Ingestion;

public class IngestionHealthCheckTests
{
    private static readonly IngestionOptions Options = new() { StaleAfter = TimeSpan.FromMinutes(2) };

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Check_RightAfterStart_IsHealthy()
    {
        var status = new IngestionStatus(_time);

        (await CheckAsync(status)).Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task Check_NoSuccessForTooLong_IsDegraded()
    {
        var status = new IngestionStatus(_time);
        _time.Advance(TimeSpan.FromMinutes(3));
        status.Record(new IngestionResult(IngestionOutcome.FetchFailed));

        (await CheckAsync(status)).Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task Check_RecentSuccess_IsHealthy()
    {
        var status = new IngestionStatus(_time);
        _time.Advance(TimeSpan.FromMinutes(10));
        status.Record(new IngestionResult(IngestionOutcome.Published, PublishedReadings: 18));
        _time.Advance(TimeSpan.FromMinutes(1));

        (await CheckAsync(status)).Status.ShouldBe(HealthStatus.Healthy);
    }

    private Task<HealthCheckResult> CheckAsync(IngestionStatus status) =>
        new IngestionHealthCheck(status, Microsoft.Extensions.Options.Options.Create(Options), _time)
            .CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);
}
