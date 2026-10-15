using System.Diagnostics.Metrics;

namespace Telemetry.Notifications.Diagnostics;

/// <summary>Real-time delivery metrics, exported through OpenTelemetry.</summary>
public sealed class NotificationsMetrics
{
    public const string MeterName = "Telemetry.Notifications";

    private readonly UpDownCounter<long> _connectedClients;
    private readonly Counter<long> _notifications;
    private readonly Counter<long> _readings;

    public NotificationsMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        var meter = meterFactory.Create(MeterName);
        _connectedClients = meter.CreateUpDownCounter<long>(
            "telemetry.notifications.clients.connected",
            unit: "{client}",
            description: "Currently connected SignalR clients.");
        _notifications = meter.CreateCounter<long>(
            "telemetry.notifications.sent",
            unit: "{notification}",
            description: "Batches broadcast to clients.");
        _readings = meter.CreateCounter<long>(
            "telemetry.notifications.readings",
            unit: "{reading}",
            description: "Readings broadcast to clients.");
    }

    public void ClientConnected() => _connectedClients.Add(1);

    public void ClientDisconnected() => _connectedClients.Add(-1);

    public void NotificationSent(int readings)
    {
        _notifications.Add(1);
        _readings.Add(readings);
    }
}
