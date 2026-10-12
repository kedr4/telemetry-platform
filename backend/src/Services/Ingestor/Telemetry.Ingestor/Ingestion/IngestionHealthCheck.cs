using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Telemetry.Ingestor.Ingestion;

/// <summary>
/// Reports <see cref="HealthStatus.Degraded"/> (not unhealthy: the service itself works, the upstream API
/// does not) when no batch was published for longer than <see cref="IngestionOptions.StaleAfter"/>.
/// </summary>
internal sealed class IngestionHealthCheck(
    IngestionStatus status,
    IOptions<IngestionOptions> options,
    TimeProvider timeProvider) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var snapshot = status.Snapshot;
        var lastProgress = snapshot.LastSuccessAt ?? status.StartedAt;
        var sinceProgress = timeProvider.GetUtcNow() - lastProgress;

        var result = sinceProgress <= options.Value.StaleAfter
            ? HealthCheckResult.Healthy($"Last outcome: {snapshot.LastOutcome?.ToString() ?? "none yet"}.")
            : HealthCheckResult.Degraded(
                $"No batch published for {sinceProgress:g}; last outcome: {snapshot.LastOutcome?.ToString() ?? "none"}.");

        return Task.FromResult(result);
    }
}
