namespace Telemetry.Ingestor.Ingestion;

public enum IngestionOutcome
{
    /// <summary>Valid readings were published.</summary>
    Published,

    /// <summary>The API returned no meters.</summary>
    Empty,

    /// <summary>The API returned a body that is not meter data.</summary>
    Corrupted,

    /// <summary>The API returned meters, but none passed validation.</summary>
    Invalid,

    /// <summary>The client-side rate limit held the request back; the API was not called.</summary>
    Throttled,

    /// <summary>The API rate limit was hit.</summary>
    RateLimited,

    /// <summary>The circuit breaker is open.</summary>
    CircuitOpen,

    /// <summary>The API could not be reached or kept failing.</summary>
    FetchFailed,

    /// <summary>Readings were fetched but could not be published to the broker.</summary>
    PublishFailed,
}
