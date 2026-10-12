namespace Telemetry.Ingestor.Ingestion;

public sealed record IngestionStatusSnapshot(
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset? LastSuccessAt,
    IngestionOutcome? LastOutcome,
    int ConsecutiveFailures,
    long PublishedBatches,
    long PublishedReadings,
    long RejectedReadings)
{
    public static IngestionStatusSnapshot Initial { get; } = new(null, null, null, 0, 0, 0, 0);
}
