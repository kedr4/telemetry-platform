using System.Diagnostics.Metrics;

namespace Telemetry.BuildingBlocks.Messaging;

/// <summary>Broker traffic metrics shared by every publishing and consuming service.</summary>
public sealed class MessagingMetrics
{
    public const string MeterName = "Telemetry.Messaging";

    private readonly Counter<long> _published;
    private readonly Counter<long> _consumed;
    private readonly Counter<long> _retries;
    private readonly Histogram<double> _handlerDuration;

    public MessagingMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        var meter = meterFactory.Create(MeterName);
        _published = meter.CreateCounter<long>(
            "telemetry.messaging.published",
            unit: "{message}",
            description: "Messages published, by message type and outcome (confirmed or failed).");
        _consumed = meter.CreateCounter<long>(
            "telemetry.messaging.consumed",
            unit: "{message}",
            description: "Messages consumed, by queue and outcome (acked, dead_lettered or requeued).");
        _retries = meter.CreateCounter<long>(
            "telemetry.messaging.handler.retries",
            unit: "{retry}",
            description: "In-process retries of failed message handlers, by queue.");
        _handlerDuration = meter.CreateHistogram<double>(
            "telemetry.messaging.handler.duration",
            unit: "ms",
            description: "Time to handle one message, retries included, by queue.");
    }

    public void Published(string messageType, bool confirmed) =>
        _published.Add(
            1,
            new KeyValuePair<string, object?>("message_type", messageType),
            new KeyValuePair<string, object?>("outcome", confirmed ? "confirmed" : "failed"));

    public void Consumed(string queue, ConsumeOutcome outcome, TimeSpan? duration = null)
    {
        var queueTag = new KeyValuePair<string, object?>("queue", queue);
        _consumed.Add(1, queueTag, new KeyValuePair<string, object?>("outcome", ToTag(outcome)));

        if (duration is { } elapsed)
        {
            _handlerDuration.Record(elapsed.TotalMilliseconds, queueTag);
        }
    }

    public void HandlerRetried(string queue) =>
        _retries.Add(1, new KeyValuePair<string, object?>("queue", queue));

    private static string ToTag(ConsumeOutcome outcome) => outcome switch
    {
        ConsumeOutcome.Acked => "acked",
        ConsumeOutcome.DeadLettered => "dead_lettered",
        ConsumeOutcome.Requeued => "requeued",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, message: null),
    };
}
