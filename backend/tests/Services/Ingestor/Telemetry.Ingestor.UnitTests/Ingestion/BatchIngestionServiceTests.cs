using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute.ExceptionExtensions;
using Telemetry.BuildingBlocks.Messaging;
using Telemetry.Contracts.Messaging;
using Telemetry.Contracts.Readings;
using Telemetry.Ingestor.Diagnostics;
using Telemetry.Ingestor.Ingestion;
using Telemetry.Ingestor.Ingestion.Validation;
using Telemetry.Ingestor.UnitTests.TestData;
using Telemetry.Ingestor.WeakApi;

namespace Telemetry.Ingestor.UnitTests.Ingestion;

public sealed class BatchIngestionServiceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly IWeakApiClient _client = Substitute.For<IWeakApiClient>();
    private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();
    private readonly FakeTimeProvider _time = new(Now);
    private readonly ServiceProvider _metricsProvider = new ServiceCollection().AddMetrics().BuildServiceProvider();
    private readonly IngestionStatus _status;
    private readonly BatchIngestionService _service;

    public BatchIngestionServiceTests()
    {
        _status = new IngestionStatus(_time);
        _service = new BatchIngestionService(
            _client,
            new MeterReadingValidator(),
            _publisher,
            _status,
            new IngestorMetrics(_metricsProvider.GetRequiredService<IMeterFactory>()),
            _time,
            NullLogger<BatchIngestionService>.Instance);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static TheoryData<FetchResult, IngestionOutcome, TimeSpan?> FailedFetches() => new()
    {
        { new FetchResult.Corrupted("data corrupted"), IngestionOutcome.Corrupted, null },
        { new FetchResult.Failed("HTTP 502"), IngestionOutcome.FetchFailed, null },
        { new FetchResult.RateLimited(TimeSpan.FromSeconds(23)), IngestionOutcome.RateLimited, TimeSpan.FromSeconds(23) },
        { new FetchResult.Throttled(TimeSpan.FromSeconds(4)), IngestionOutcome.Throttled, TimeSpan.FromSeconds(4) },
        { new FetchResult.CircuitOpen(TimeSpan.FromSeconds(10)), IngestionOutcome.CircuitOpen, TimeSpan.FromSeconds(10) },
    };

    public void Dispose() => _metricsProvider.Dispose();

    [Fact]
    public async Task IngestAsync_ValidMeters_PublishesStampedBatch()
    {
        ApiReturns(new FetchResult.Success([Meters.Energy(), Meters.AirQuality(), Meters.Motion()]));

        var result = await _service.IngestAsync(CancellationToken);

        result.ShouldBe(new IngestionResult(IngestionOutcome.Published, PublishedReadings: 3));
        var batch = PublishedBatch();
        batch.FetchedAt.ShouldBe(Now);
        batch.BatchId.Version.ShouldBe(7);
        batch.Readings.Select(r => r.Type).ShouldBe([MeterType.Energy, MeterType.AirQuality, MeterType.Motion]);
    }

    [Fact]
    public async Task IngestAsync_SomeInvalidMeters_PublishesOnlyValidOnes()
    {
        ApiReturns(new FetchResult.Success(
        [
            Meters.Energy("Kitchen"),
            Meters.Energy("Bedroom", energy: -10),
            Meters.Create("temperature", "Office", """{ "celsius": 21 }"""),
            Meters.Create("motion", "Office", """{ "motionDetected": "maybe" }"""),
        ]));

        var result = await _service.IngestAsync(CancellationToken);

        result.ShouldBe(new IngestionResult(IngestionOutcome.Published, PublishedReadings: 1, RejectedReadings: 3));
        PublishedBatch().Readings.ShouldBe([new EnergyReading("Kitchen", 604.88)]);
    }

    [Fact]
    public async Task IngestAsync_DuplicateMeters_KeepsFirstOccurrence()
    {
        ApiReturns(new FetchResult.Success([Meters.Energy("Kitchen", 1), Meters.Energy("Kitchen", 2)]));

        var result = await _service.IngestAsync(CancellationToken);

        result.RejectedReadings.ShouldBe(1);
        PublishedBatch().Readings.ShouldBe([new EnergyReading("Kitchen", 1)]);
    }

    [Fact]
    public async Task IngestAsync_EmptyList_DoesNotPublish()
    {
        ApiReturns(new FetchResult.Success([]));

        var result = await _service.IngestAsync(CancellationToken);

        result.Outcome.ShouldBe(IngestionOutcome.Empty);
        await NothingPublishedAsync();
    }

    [Fact]
    public async Task IngestAsync_OnlyInvalidMeters_DoesNotPublish()
    {
        ApiReturns(new FetchResult.Success([Meters.Energy(energy: -1), Meters.AirQuality(humidity: 900)]));

        var result = await _service.IngestAsync(CancellationToken);

        result.ShouldBe(new IngestionResult(IngestionOutcome.Invalid, RejectedReadings: 2));
        await NothingPublishedAsync();
    }

    [Theory]
    [MemberData(nameof(FailedFetches))]
    public async Task IngestAsync_FailedFetch_DoesNotPublish(FetchResult fetch, IngestionOutcome expected, TimeSpan? retryAfter)
    {
        ApiReturns(fetch);

        var result = await _service.IngestAsync(CancellationToken);

        result.ShouldBe(new IngestionResult(expected, RetryAfter: retryAfter));
        await NothingPublishedAsync();
    }

    [Fact]
    public async Task IngestAsync_PublishFails_ReportsPublishFailed()
    {
        ApiReturns(new FetchResult.Success([Meters.Energy()]));
        _publisher.PublishAsync(Arg.Any<MeterBatchFetched>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Broker unavailable"));

        var result = await _service.IngestAsync(CancellationToken);

        result.Outcome.ShouldBe(IngestionOutcome.PublishFailed);
    }

    [Fact]
    public async Task IngestAsync_UpdatesStatus()
    {
        ApiReturns(new FetchResult.Failed("HTTP 502"));
        await _service.IngestAsync(CancellationToken);
        _status.Snapshot.ConsecutiveFailures.ShouldBe(1);
        _status.Snapshot.LastSuccessAt.ShouldBeNull();

        ApiReturns(new FetchResult.Success([Meters.Energy(), Meters.Motion()]));
        await _service.IngestAsync(CancellationToken);

        var snapshot = _status.Snapshot;
        snapshot.ConsecutiveFailures.ShouldBe(0);
        snapshot.LastSuccessAt.ShouldBe(Now);
        snapshot.LastOutcome.ShouldBe(IngestionOutcome.Published);
        snapshot.PublishedBatches.ShouldBe(1);
        snapshot.PublishedReadings.ShouldBe(2);
    }

    private void ApiReturns(FetchResult result) =>
        _client.GetMetersAsync(Arg.Any<CancellationToken>()).Returns(result);

    private MeterBatchFetched PublishedBatch()
    {
        var call = _publisher.ReceivedCalls().ShouldHaveSingleItem();
        return call.GetArguments()[0].ShouldBeOfType<MeterBatchFetched>();
    }

    private Task NothingPublishedAsync() =>
        _publisher.DidNotReceive().PublishAsync(Arg.Any<MeterBatchFetched>(), Arg.Any<CancellationToken>());
}
