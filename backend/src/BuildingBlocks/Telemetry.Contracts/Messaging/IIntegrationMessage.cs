namespace Telemetry.Contracts.Messaging;

/// <summary>
/// A message exchanged between services through the message broker.
/// </summary>
public interface IIntegrationMessage
{
    /// <summary>Gets the routing key the message is published with.</summary>
    static abstract string RoutingKey { get; }
}
