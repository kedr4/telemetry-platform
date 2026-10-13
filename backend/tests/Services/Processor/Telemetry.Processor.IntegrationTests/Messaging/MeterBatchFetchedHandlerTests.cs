using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Telemetry.BuildingBlocks.Messaging;
using Telemetry.Contracts.Messaging;
using Telemetry.Contracts.Readings;
using Telemetry.Processor.Diagnostics;
using Telemetry.Processor.IntegrationTests.Infrastructure;
using Telemetry.Processor.Messaging;
using Telemetry.Processor.Persistence;

namespace Telemetry.Processor.IntegrationTests.Messaging;

/// <summary>The handler against a real, migrated PostgreSQL database.</summary>
public sealed class MeterBatchFetchedHandlerTests(MigratedDatabaseFixture database) : IDisposable
{
    private static readonly MessageContext Context = new("message-1", "correlation-1", Redelivered: false);

    private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();
    private readonly ServiceProvider _metricsProvider = new ServiceCollection().AddMetrics().BuildServiceProvider();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose() => _metricsProvider.Dispose();

    [Fact]
    public async Task NewBatch_StoresMetersBatchAndTypedReadings_AndPublishesEvent()
    {
        var location = Batches.UniqueLocation("Kitchen");
        var message = Batches.ForLocation(location);

        await HandleAsync(message);

        await using var db = database.CreateDbContext();
        (await db.Meters.CountAsync(m => m.Location == location, CancellationToken)).ShouldBe(3);
        var batch = await db.Batches.SingleAsync(b => b.Id == message.BatchId, CancellationToken);
        batch.ReadingCount.ShouldBe(3);

        var readings = await db.Readings.Include(r => r.Meter)
            .Where(r => r.BatchId == message.BatchId)
            .ToDictionaryAsync(r => r.Meter.Type, CancellationToken);
        readings[MeterType.Energy].Energy.ShouldBe(604.88);
        (readings[MeterType.AirQuality].Co2, readings[MeterType.AirQuality].Pm25, readings[MeterType.AirQuality].Humidity)
            .ShouldBe((543, 26, 68));
        readings[MeterType.Motion].MotionDetected.ShouldBe(true);
        readings.Values.ShouldAllBe(r => r.RecordedAt == batch.FetchedAt);

        await _publisher.Received(1).PublishAsync(
            Arg.Is<ReadingsPersisted>(e => e.BatchId == message.BatchId && e.Readings.Count == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LaterBatch_ReusesExistingMeters()
    {
        var location = Batches.UniqueLocation("Garage");

        await HandleAsync(Batches.ForLocation(location));
        await HandleAsync(Batches.ForLocation(location));

        await using var db = database.CreateDbContext();
        (await db.Meters.CountAsync(m => m.Location == location, CancellationToken)).ShouldBe(3);
        (await db.Readings.CountAsync(r => r.Meter.Location == location, CancellationToken)).ShouldBe(6);
    }

    [Fact]
    public async Task RedeliveredBatch_IsStoredOnce_ButAnnouncedAgain()
    {
        var message = Batches.ForLocation(Batches.UniqueLocation("Office"));

        await HandleAsync(message);
        await HandleAsync(message);

        await using var db = database.CreateDbContext();
        (await db.Readings.CountAsync(r => r.BatchId == message.BatchId, CancellationToken)).ShouldBe(3);
        await _publisher.Received(2).PublishAsync(
            Arg.Is<ReadingsPersisted>(e => e.BatchId == message.BatchId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConcurrentDeliveriesOfTheSameBatch_StoreItOnce()
    {
        var message = Batches.ForLocation(Batches.UniqueLocation("Bedroom"));

        // Separate contexts, like two consumer instances; one of them loses the race on the primary key.
        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => HandleAsync(message)));

        await using var db = database.CreateDbContext();
        (await db.Batches.CountAsync(b => b.Id == message.BatchId, CancellationToken)).ShouldBe(1);
        (await db.Readings.CountAsync(r => r.BatchId == message.BatchId, CancellationToken)).ShouldBe(3);
    }

    [Fact]
    public async Task EmptyBatch_IsRejectedAsNonRetryable()
    {
        await Should.ThrowAsync<NonRetryableMessageException>(() => HandleAsync(Batches.Create()));
    }

    [Fact]
    public async Task BatchWithTheSameMeterTwice_IsRejectedAsNonRetryable()
    {
        var location = Batches.UniqueLocation("Corridor");

        await Should.ThrowAsync<NonRetryableMessageException>(() =>
            HandleAsync(Batches.Create(new EnergyReading(location, 1), new EnergyReading(location, 2))));
    }

    [Fact]
    public async Task BatchViolatingDomainRules_IsRejectedAsNonRetryable()
    {
        await Should.ThrowAsync<NonRetryableMessageException>(() =>
            HandleAsync(Batches.Create(new EnergyReading(" padded location ", 1))));
    }

    private async Task HandleAsync(MeterBatchFetched message)
    {
        await using var db = database.CreateDbContext();
        var handler = new MeterBatchFetchedHandler(
            db,
            _publisher,
            new ProcessorMetrics(_metricsProvider.GetRequiredService<IMeterFactory>()),
            TimeProvider.System,
            NullLogger<MeterBatchFetchedHandler>.Instance);

        await handler.HandleAsync(message, Context, CancellationToken);
    }
}
