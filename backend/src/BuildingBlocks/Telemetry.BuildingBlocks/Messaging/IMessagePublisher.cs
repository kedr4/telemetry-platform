using Telemetry.Contracts.Messaging;

namespace Telemetry.BuildingBlocks.Messaging;

public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a persistent message and waits for the broker to confirm it was routed to a queue.
    /// </summary>
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : IIntegrationMessage;
}
