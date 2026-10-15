namespace Telemetry.Notifications.Hubs;

/// <summary>New readings were stored.</summary>
/// <param name="BatchId">Identifier of the stored batch; lets clients ignore a repeated notification.</param>
/// <param name="RecordedAt">When the readings were taken.</param>
/// <param name="Readings">The stored readings.</param>
public sealed record ReadingsUpdatedNotification(
    Guid BatchId,
    DateTimeOffset RecordedAt,
    IReadOnlyList<ReadingNotification> Readings);
