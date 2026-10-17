namespace Telemetry.BuildingBlocks.Messaging;

/// <summary>What happened to a delivered message.</summary>
public enum ConsumeOutcome
{
    Acked,
    DeadLettered,
    Requeued,
}
