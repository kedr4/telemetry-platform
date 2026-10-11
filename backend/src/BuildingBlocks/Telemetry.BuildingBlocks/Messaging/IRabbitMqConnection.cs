using RabbitMQ.Client;

namespace Telemetry.BuildingBlocks.Messaging;

/// <summary>
/// The single, automatically recovering broker connection of a service.
/// </summary>
public interface IRabbitMqConnection
{
    bool IsConnected { get; }

    /// <summary>Returns the connection, establishing it (with retries) on first use.</summary>
    Task<IConnection> GetAsync(CancellationToken cancellationToken);
}
