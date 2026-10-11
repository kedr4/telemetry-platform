using Telemetry.Contracts.Messaging;

namespace Telemetry.BuildingBlocks.Messaging;

/// <summary>
/// Handles messages of one type. Resolved from a fresh DI scope for every delivery.
/// Throw <see cref="NonRetryableMessageException"/> to dead-letter a message without retrying.
/// </summary>
public interface IMessageHandler<in TMessage>
    where TMessage : IIntegrationMessage
{
    Task HandleAsync(TMessage message, MessageContext context, CancellationToken cancellationToken);
}
