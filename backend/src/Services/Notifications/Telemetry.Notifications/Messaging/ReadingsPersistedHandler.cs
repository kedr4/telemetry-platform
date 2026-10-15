using Microsoft.AspNetCore.SignalR;
using Telemetry.BuildingBlocks.Messaging;
using Telemetry.Contracts.Messaging;
using Telemetry.Notifications.Diagnostics;
using Telemetry.Notifications.Hubs;

namespace Telemetry.Notifications.Messaging;

/// <summary>
/// Forwards stored readings to SignalR clients: the whole batch to everyone, and each location's readings
/// to the clients subscribed to that location.
/// </summary>
internal sealed partial class ReadingsPersistedHandler(
    IHubContext<TelemetryHub, ITelemetryClient> hub,
    NotificationsMetrics metrics,
    ILogger<ReadingsPersistedHandler> logger) : IMessageHandler<ReadingsPersisted>
{
    public async Task HandleAsync(ReadingsPersisted message, MessageContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var notification = new ReadingsUpdatedNotification(
            message.BatchId,
            message.RecordedAt,
            message.Readings.Select(reading => ReadingNotification.From(reading, message.RecordedAt)).ToList());

        await hub.Clients.All.ReadingsUpdated(notification);

        foreach (var location in notification.Readings.GroupBy(reading => reading.Location, StringComparer.Ordinal))
        {
            await hub.Clients.Group(TelemetryHub.LocationGroup(location.Key)).LocationReadingsUpdated(
                location.Key,
                notification with { Readings = location.ToList() });
        }

        metrics.NotificationSent(notification.Readings.Count);
        LogBroadcast(message.BatchId, notification.Readings.Count);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Broadcast batch {BatchId} with {Readings} readings")]
    private partial void LogBroadcast(Guid batchId, int readings);
}
