namespace Telemetry.Notifications.Hubs;

/// <summary>Methods the server invokes on connected clients (strongly typed SignalR hub).</summary>
public interface ITelemetryClient
{
    /// <summary>Sent to every client whenever a batch of readings has been stored.</summary>
    Task ReadingsUpdated(ReadingsUpdatedNotification notification);

    /// <summary>Sent to clients subscribed to <paramref name="location"/>, with only that location's readings.</summary>
    Task LocationReadingsUpdated(string location, ReadingsUpdatedNotification notification);
}
