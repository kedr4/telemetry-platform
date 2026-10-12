namespace Telemetry.Ingestor.Ingestion;

/// <summary>
/// Thread-safe record of recent ingestion activity, exposed through the status endpoint and health check.
/// </summary>
public sealed class IngestionStatus(TimeProvider timeProvider)
{
    private readonly Lock _lock = new();
    private readonly DateTimeOffset _startedAt = timeProvider.GetUtcNow();
    private IngestionStatusSnapshot _snapshot = IngestionStatusSnapshot.Initial;

    public DateTimeOffset StartedAt => _startedAt;

    public IngestionStatusSnapshot Snapshot
    {
        get
        {
            lock (_lock)
            {
                return _snapshot;
            }
        }
    }

    public void Record(IngestionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var now = timeProvider.GetUtcNow();

        lock (_lock)
        {
            _snapshot = _snapshot with
            {
                LastAttemptAt = now,
                LastOutcome = result.Outcome,
                LastSuccessAt = result.IsSuccess ? now : _snapshot.LastSuccessAt,
                ConsecutiveFailures = result.IsSuccess ? 0 : _snapshot.ConsecutiveFailures + 1,
                PublishedBatches = _snapshot.PublishedBatches + (result.IsSuccess ? 1 : 0),
                PublishedReadings = _snapshot.PublishedReadings + result.PublishedReadings,
                RejectedReadings = _snapshot.RejectedReadings + result.RejectedReadings,
            };
        }
    }
}
