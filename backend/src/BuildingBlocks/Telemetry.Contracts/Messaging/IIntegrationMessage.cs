namespace Telemetry.Contracts.Messaging;

/// <summary>
/// A message exchanged between services through the message broker.
/// </summary>
public interface IIntegrationMessage
{
    /// <summary>Gets the routing key the message is published with.</summary>
    static abstract string RoutingKey { get; }

    /// <summary>
    /// Gets a value indicating whether publishing must fail when no queue receives the message.
    /// <c>true</c> for work that has to be processed; <c>false</c> for broadcast events that may
    /// legitimately have no subscribers.
    /// </summary>
    static virtual bool RequiresSubscriber => true;
}
