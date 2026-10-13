using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Telemetry.BuildingBlocks.Messaging;
using Telemetry.Contracts.Messaging;
using Telemetry.Contracts.Readings;
using Telemetry.Processor.Diagnostics;
using Telemetry.Processor.Domain;
using Telemetry.Processor.Persistence;

namespace Telemetry.Processor.Messaging;

/// <summary>
/// Stores a fetched batch exactly once and announces it with <see cref="ReadingsPersisted"/>.
/// <para>
/// Delivery is at-least-once, so the batch id is the idempotency key: a batch that is already stored is
/// not stored again. Its <see cref="ReadingsPersisted"/> event is published again though, because the
/// earlier attempt may have stored the batch and then failed before publishing. Notifications are
/// therefore at-least-once as well, which is harmless for "new data available" signals.
/// </para>
/// </summary>
internal sealed partial class MeterBatchFetchedHandler(
    ProcessorDbContext db,
    IMessagePublisher publisher,
    ProcessorMetrics metrics,
    TimeProvider timeProvider,
    ILogger<MeterBatchFetchedHandler> logger) : IMessageHandler<MeterBatchFetched>
{
    public async Task HandleAsync(MeterBatchFetched message, MessageContext context, CancellationToken cancellationToken)
    {
        EnsureWellFormed(message);
        var stopwatch = Stopwatch.StartNew();

        var stored = await StoreAsync(message, cancellationToken);

        metrics.RecordBatch(stored, stored ? message.Readings.Count : 0, stopwatch.Elapsed);
        if (stored)
        {
            LogStored(message.BatchId, message.Readings.Count);
        }
        else
        {
            LogDuplicate(message.BatchId, context.Redelivered);
        }

        await publisher.PublishAsync(
            new ReadingsPersisted(message.BatchId, message.FetchedAt, message.Readings),
            cancellationToken);
    }

    private static void EnsureWellFormed(MeterBatchFetched message)
    {
        if (message.Readings.Count == 0)
        {
            throw new NonRetryableMessageException($"Batch {message.BatchId} contains no readings.");
        }

        var distinctMeters = message.Readings.Select(reading => (reading.Location, reading.Type)).Distinct().Count();
        if (distinctMeters != message.Readings.Count)
        {
            throw new NonRetryableMessageException($"Batch {message.BatchId} contains the same meter more than once.");
        }
    }

    private static bool IsDuplicateBatch(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "pk_meter_batches",
        };

    /// <returns><c>true</c> if the batch was stored; <c>false</c> if it had been stored before.</returns>
    private async Task<bool> StoreAsync(MeterBatchFetched message, CancellationToken cancellationToken)
    {
        if (await db.Batches.AnyAsync(batch => batch.Id == message.BatchId, cancellationToken))
        {
            return false;
        }

        try
        {
            var meters = await GetOrAddMetersAsync(message.Readings, cancellationToken);
            var batch = MeterBatch.Create(message.BatchId, message.FetchedAt, timeProvider.GetUtcNow(), message.Readings.Count);

            db.Batches.Add(batch);
            db.Readings.AddRange(message.Readings.Select(reading =>
                Reading.Record(meters[(reading.Location, reading.Type)], batch.Id, batch.FetchedAt, reading)));

            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (ArgumentException ex)
        {
            // Domain invariants violated: retrying cannot help.
            throw new NonRetryableMessageException($"Batch {message.BatchId} is invalid: {ex.Message}", ex);
        }
        catch (DbUpdateException ex) when (IsDuplicateBatch(ex))
        {
            // Another consumer stored the same batch concurrently.
            return false;
        }
    }

    private async Task<Dictionary<(string Location, MeterType Type), Meter>> GetOrAddMetersAsync(
        IReadOnlyList<MeterReading> readings,
        CancellationToken cancellationToken)
    {
        var locations = readings.Select(reading => reading.Location).Distinct().ToList();
        var meters = await db.Meters
            .Where(meter => locations.Contains(meter.Location))
            .ToDictionaryAsync(meter => (meter.Location, meter.Type), cancellationToken);

        foreach (var reading in readings)
        {
            var key = (reading.Location, reading.Type);
            if (!meters.ContainsKey(key))
            {
                var meter = Meter.Create(reading.Location, reading.Type);
                db.Meters.Add(meter);
                meters[key] = meter;
                LogNewMeter(reading.Type, reading.Location);
            }
        }

        return meters;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Stored batch {BatchId} with {Readings} readings")]
    private partial void LogStored(Guid batchId, int readings);

    [LoggerMessage(Level = LogLevel.Information, Message = "Batch {BatchId} was already stored (redelivered: {Redelivered}); skipping")]
    private partial void LogDuplicate(Guid batchId, bool redelivered);

    [LoggerMessage(Level = LogLevel.Information, Message = "Registered new {MeterType} meter in {Location}")]
    private partial void LogNewMeter(MeterType meterType, string location);
}
