namespace Telemetry.Ingestor.WeakApi;

public interface IWeakApiClient
{
    /// <summary>
    /// Fetches the current meter readings. Never throws for upstream failures; only for cancellation.
    /// </summary>
    Task<FetchResult> GetMetersAsync(CancellationToken cancellationToken);
}
