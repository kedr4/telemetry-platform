namespace Telemetry.Ingestor.Ingestion;

/// <summary>Result of one ingestion cycle.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="PublishedReadings">Number of readings published.</param>
/// <param name="RejectedReadings">Number of readings dropped by mapping/validation.</param>
/// <param name="RetryAfter">How long the upstream API asked us to wait, if it did.</param>
public sealed record IngestionResult(
    IngestionOutcome Outcome,
    int PublishedReadings = 0,
    int RejectedReadings = 0,
    TimeSpan? RetryAfter = null)
{
    public bool IsSuccess => Outcome == IngestionOutcome.Published;
}
