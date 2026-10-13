using Telemetry.Contracts.Messaging;

namespace Telemetry.BuildingBlocks.IntegrationTests.Messaging;

/// <summary>Broadcast event that no queue is bound to.</summary>
/// <param name="Id">Payload.</param>
public sealed record UnroutedEvent(int Id) : IIntegrationMessage
{
    public static string RoutingKey => "tests.unrouted-event";

    public static bool RequiresSubscriber => false;
}
