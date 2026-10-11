using Telemetry.Contracts.Readings;

namespace Telemetry.Contracts.Messaging;

/// <summary>
/// Published by the Processor once a batch of readings has been stored.
/// </summary>
/// <param name="BatchId">Identifier of the stored batch.</param>
/// <param name="RecordedAt">Timestamp the readings were stored with.</param>
/// <param name="Readings">The stored readings.</param>
public sealed record ReadingsPersisted(
    Guid BatchId,
    DateTimeOffset RecordedAt,
    IReadOnlyList<MeterReading> Readings) : IIntegrationMessage
{
    public static string RoutingKey => "readings.persisted";
}
