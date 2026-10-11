using Telemetry.BuildingBlocks.Messaging;
using Telemetry.Contracts.Messaging;

namespace Telemetry.BuildingBlocks.IntegrationTests.Infrastructure;

internal sealed class ProbeHandler(HandlerProbe probe) : IMessageHandler<ReadingsPersisted>
{
    public Task HandleAsync(ReadingsPersisted message, MessageContext context, CancellationToken cancellationToken)
    {
        probe.RecordAttempt();

        if (probe.Failure is not null)
        {
            throw probe.Failure();
        }

        probe.Handled.Enqueue((message, context));
        return Task.CompletedTask;
    }
}
