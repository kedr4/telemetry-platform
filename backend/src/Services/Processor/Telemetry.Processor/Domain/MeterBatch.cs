namespace Telemetry.Processor.Domain;

/// <summary>
/// One fetch from the upstream API. Its identifier makes persistence idempotent: a batch is stored once.
/// </summary>
public sealed class MeterBatch
{
    private MeterBatch(Guid id, DateTimeOffset fetchedAt, DateTimeOffset processedAt, int readingCount)
    {
        Id = id;
        FetchedAt = fetchedAt;
        ProcessedAt = processedAt;
        ReadingCount = readingCount;
    }

    public Guid Id { get; private set; }

    /// <summary>Gets when the readings were fetched, i.e. when they were measured.</summary>
    public DateTimeOffset FetchedAt { get; private set; }

    /// <summary>Gets when the batch was stored.</summary>
    public DateTimeOffset ProcessedAt { get; private set; }

    public int ReadingCount { get; private set; }

    public static MeterBatch Create(Guid id, DateTimeOffset fetchedAt, DateTimeOffset processedAt, int readingCount)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Batch id must not be empty.", nameof(id));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(readingCount);

        // PostgreSQL 'timestamp with time zone' stores UTC instants.
        return new MeterBatch(id, fetchedAt.ToUniversalTime(), processedAt.ToUniversalTime(), readingCount);
    }
}
