using Telemetry.Gateway.Application.Common;
using Telemetry.Gateway.Application.Readings;
using Telemetry.Gateway.Domain.Aggregations;
using Telemetry.Gateway.Domain.Meters;
using Telemetry.Gateway.Domain.Readings;

namespace Telemetry.Gateway.Application.Abstractions;

/// <summary>Read access to stored telemetry. Implemented by the infrastructure layer.</summary>
public interface ITelemetryReadStore
{
    Task<IReadOnlyList<Meter>> GetMetersAsync(IReadOnlyCollection<MeterType> types, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetLocationsAsync(CancellationToken cancellationToken);

    /// <summary>Returns the most recent reading of every meter matching the filter.</summary>
    Task<IReadOnlyList<Reading>> GetLatestReadingsAsync(ReadingFilter filter, CancellationToken cancellationToken);

    /// <summary>Returns up to <paramref name="pageSize"/> readings after <paramref name="after"/>, newest first.</summary>
    Task<ReadingSlice> GetReadingsAsync(
        ReadingFilter filter,
        ReadingCursor? after,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AggregateResult>> GetAggregatesAsync(AggregationSpec spec, CancellationToken cancellationToken);
}
