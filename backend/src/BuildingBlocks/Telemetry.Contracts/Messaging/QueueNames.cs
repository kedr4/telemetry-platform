namespace Telemetry.Contracts.Messaging;

/// <summary>
/// Names of the durable queues owned by the consuming services.
/// </summary>
public static class QueueNames
{
    public const string ProcessorMeterBatches = "processor.meter-batches";
    public const string NotificationsReadingsPersisted = "notifications.readings-persisted";
}
