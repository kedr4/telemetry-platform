using Telemetry.Contracts.Messaging;

namespace Telemetry.BuildingBlocks.IntegrationTests.Messaging;

/// <summary>Work item that no queue is bound to.</summary>
/// <param name="Id">Payload.</param>
public sealed record UnroutedWorkItem(int Id) : IIntegrationMessage
{
    public static string RoutingKey => "tests.unrouted-work-item";
}
