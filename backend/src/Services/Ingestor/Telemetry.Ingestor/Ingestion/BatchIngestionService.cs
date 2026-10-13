using FluentValidation;
using Telemetry.BuildingBlocks.Messaging;
using Telemetry.Contracts.Messaging;
using Telemetry.Contracts.Readings;
using Telemetry.Ingestor.Diagnostics;
using Telemetry.Ingestor.WeakApi;

namespace Telemetry.Ingestor.Ingestion;

/// <summary>
/// One ingestion cycle: fetch → map → validate → de-duplicate → stamp → publish.
/// </summary>
internal sealed partial class BatchIngestionService(
    IWeakApiClient client,
    IValidator<MeterReading> validator,
    IMessagePublisher publisher,
    IngestionStatus status,
    IngestorMetrics metrics,
    TimeProvider timeProvider,
    ILogger<BatchIngestionService> logger)
{
    public async Task<IngestionResult> IngestAsync(CancellationToken cancellationToken)
    {
        var fetch = await client.GetMetersAsync(cancellationToken);

        var result = fetch switch
        {
            FetchResult.Success success => await PublishAsync(success.Meters, cancellationToken),
            FetchResult.Corrupted corrupted => Skipped(IngestionOutcome.Corrupted, corrupted.Reason),
            FetchResult.Throttled throttled => Throttled(throttled.RetryAfter),
            FetchResult.RateLimited limited => Skipped(
                IngestionOutcome.RateLimited,
                $"WeakApp rate limit exceeded; retry after {limited.RetryAfter?.ToString() ?? "unspecified"}.",
                limited.RetryAfter),
            FetchResult.CircuitOpen open => Skipped(IngestionOutcome.CircuitOpen, "Circuit breaker is open.", open.RetryAfter),
            FetchResult.Failed failed => Skipped(IngestionOutcome.FetchFailed, failed.Reason),
            _ => throw new InvalidOperationException($"Unhandled fetch result {fetch.GetType().Name}."),
        };

        status.Record(result);
        metrics.Record(result);
        return result;
    }

    private async Task<IngestionResult> PublishAsync(IReadOnlyList<WeakApiMeterDto> meters, CancellationToken cancellationToken)
    {
        if (meters.Count == 0)
        {
            return Skipped(IngestionOutcome.Empty, "The API returned no meters.");
        }

        var readings = SelectValidReadings(meters);
        var rejected = meters.Count - readings.Count;

        if (readings.Count == 0)
        {
            return Skipped(IngestionOutcome.Invalid, "No meter passed validation.") with { RejectedReadings = rejected };
        }

        var now = timeProvider.GetUtcNow();
        var batch = new MeterBatchFetched(Guid.CreateVersion7(now), now, readings);

        try
        {
            await publisher.PublishAsync(batch, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogPublishFailed(ex, batch.BatchId);
            return new IngestionResult(IngestionOutcome.PublishFailed, RejectedReadings: rejected);
        }

        LogPublished(batch.BatchId, readings.Count, rejected);
        return new IngestionResult(IngestionOutcome.Published, readings.Count, rejected);
    }

    private List<MeterReading> SelectValidReadings(IReadOnlyList<WeakApiMeterDto> meters)
    {
        var readings = new List<MeterReading>(meters.Count);
        var seen = new HashSet<(string Location, MeterType Type)>();

        foreach (var meter in meters)
        {
            if (!ReadingMapper.TryMap(meter, out var reading, out var mappingError))
            {
                LogRejected(mappingError);
                continue;
            }

            var validation = validator.Validate(reading);
            if (!validation.IsValid)
            {
                LogRejected($"'{meter.Type}' meter '{reading.Location}': {validation}");
                continue;
            }

            if (!seen.Add((reading.Location, reading.Type)))
            {
                LogRejected($"Duplicate '{meter.Type}' meter '{reading.Location}' in batch.");
                continue;
            }

            readings.Add(reading);
        }

        return readings;
    }

    // Expected while the client-side limiter is exhausted, so not a warning.
    private IngestionResult Throttled(TimeSpan? retryAfter)
    {
        if (retryAfter is { } delay)
        {
            LogThrottledWithDelay(delay);
        }
        else
        {
            LogThrottled();
        }

        return new IngestionResult(IngestionOutcome.Throttled, RetryAfter: retryAfter);
    }

    private IngestionResult Skipped(IngestionOutcome outcome, string reason, TimeSpan? retryAfter = null)
    {
        LogSkipped(outcome, reason);
        return new IngestionResult(outcome, RetryAfter: retryAfter);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Published batch {BatchId} with {Readings} readings ({Rejected} rejected)")]
    private partial void LogPublished(Guid batchId, int readings, int rejected);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Ingestion cycle skipped: {Outcome}. {Reason}")]
    private partial void LogSkipped(IngestionOutcome outcome, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Ingestion cycle throttled by the client-side rate limit; retrying at the next poll")]
    private partial void LogThrottled();

    [LoggerMessage(Level = LogLevel.Information, Message = "Ingestion cycle throttled by the client-side rate limit; retrying in {RetryAfter}")]
    private partial void LogThrottledWithDelay(TimeSpan retryAfter);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Reading rejected: {Reason}")]
    private partial void LogRejected(string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "Publishing batch {BatchId} failed")]
    private partial void LogPublishFailed(Exception exception, Guid batchId);
}
