using Telemetry.Contracts.Readings;

namespace Telemetry.Contracts.Messaging;

/// <summary>
/// Published by the Ingestor after a successful, validated fetch from the upstream API.
/// </summary>
/// <param name="BatchId">Unique identifier of the fetch; used for idempotent persistence.</param>
/// <param name="FetchedAt">Moment the batch was fetched (the upstream API provides no timestamps).</param>
/// <param name="Readings">Valid readings contained in the batch.</param>
public sealed record MeterBatchFetched(
    Guid BatchId,
    DateTimeOffset FetchedAt,
    IReadOnlyList<MeterReading> Readings) : IIntegrationMessage
{
    public static string RoutingKey => "meters.batch-fetched";
}
