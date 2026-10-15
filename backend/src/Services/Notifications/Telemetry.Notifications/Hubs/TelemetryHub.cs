using Microsoft.AspNetCore.SignalR;
using Telemetry.Notifications.Diagnostics;

namespace Telemetry.Notifications.Hubs;

/// <summary>
/// Real-time telemetry hub. Every client receives <see cref="ITelemetryClient.ReadingsUpdated"/>; clients can
/// additionally subscribe to individual locations to receive <see cref="ITelemetryClient.LocationReadingsUpdated"/>.
/// </summary>
public sealed partial class TelemetryHub(NotificationsMetrics metrics, ILogger<TelemetryHub> logger) : Hub<ITelemetryClient>
{
    public const string Path = "/hubs/telemetry";
    public const int MaxLocationLength = 64;

    public static string LocationGroup(string location) => $"location:{location}";

    public Task SubscribeToLocation(string location)
    {
        Validate(location);
        LogSubscribed(Context.ConnectionId, location);
        return Groups.AddToGroupAsync(Context.ConnectionId, LocationGroup(location), Context.ConnectionAborted);
    }

    public Task UnsubscribeFromLocation(string location)
    {
        Validate(location);
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, LocationGroup(location), Context.ConnectionAborted);
    }

    public override Task OnConnectedAsync()
    {
        metrics.ClientConnected();
        LogConnected(Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        metrics.ClientDisconnected();
        LogDisconnected(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    // HubException messages are sent to the caller; other exceptions are hidden from clients.
    private static void Validate(string location)
    {
        if (string.IsNullOrWhiteSpace(location) || location.Length > MaxLocationLength)
        {
            throw new HubException($"Location must be a non-empty name of at most {MaxLocationLength} characters.");
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Client {ConnectionId} connected")]
    private partial void LogConnected(string connectionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Client {ConnectionId} disconnected")]
    private partial void LogDisconnected(string connectionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Client {ConnectionId} subscribed to {Location}")]
    private partial void LogSubscribed(string connectionId, string location);
}
