namespace Telemetry.Ingestor.WeakApi;

/// <summary>
/// Outcome of fetching meters from the upstream API. Expected failures are values, not exceptions,
/// because with this API failure is the normal case.
/// </summary>
public abstract record FetchResult
{
    private FetchResult()
    {
    }

    /// <summary>The API answered with a list of meters (possibly empty).</summary>
    public sealed record Success(IReadOnlyList<WeakApiMeterDto> Meters) : FetchResult;

    /// <summary>The API answered successfully but the body is not usable data.</summary>
    public sealed record Corrupted(string Reason) : FetchResult;

    /// <summary>The API rejected the request because of its rate limit.</summary>
    public sealed record RateLimited(TimeSpan? RetryAfter) : FetchResult;

    /// <summary>The client-side rate limit is exhausted; the API was not called.</summary>
    public sealed record Throttled(TimeSpan? RetryAfter) : FetchResult;

    /// <summary>The circuit breaker is open; the API is not called until it closes.</summary>
    public sealed record CircuitOpen(TimeSpan? RetryAfter) : FetchResult;

    /// <summary>The request failed even after retries.</summary>
    public sealed record Failed(string Reason) : FetchResult;
}
