using Telemetry.Contracts.Messaging;

namespace Telemetry.BuildingBlocks.Messaging;

public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a persistent message and waits for the broker to confirm it. Messages that require a
    /// subscriber fail when no queue is bound to their routing key.
    /// </summary>
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : IIntegrationMessage;
}
