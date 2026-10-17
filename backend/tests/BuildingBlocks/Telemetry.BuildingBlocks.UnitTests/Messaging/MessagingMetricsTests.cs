using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Telemetry.BuildingBlocks.Messaging;

namespace Telemetry.BuildingBlocks.UnitTests.Messaging;

public sealed class MessagingMetricsTests : IDisposable
{
    private readonly ServiceProvider _services = new ServiceCollection().AddMetrics().BuildServiceProvider();

    private IMeterFactory MeterFactory => _services.GetRequiredService<IMeterFactory>();

    [Theory]
    [InlineData(true, "confirmed")]
    [InlineData(false, "failed")]
    public void Published_CountsByMessageTypeAndOutcome(bool confirmed, string expectedOutcome)
    {
        using var collector = new MetricCollector<long>(MeterFactory, MessagingMetrics.MeterName, "telemetry.messaging.published");
        var metrics = new MessagingMetrics(MeterFactory);

        metrics.Published("MeterBatchFetched", confirmed);

        var measurement = collector.LastMeasurement.ShouldNotBeNull();
        measurement.Value.ShouldBe(1);
        measurement.Tags["message_type"].ShouldBe("MeterBatchFetched");
        measurement.Tags["outcome"].ShouldBe(expectedOutcome);
    }

    [Theory]
    [InlineData(ConsumeOutcome.Acked, "acked")]
    [InlineData(ConsumeOutcome.DeadLettered, "dead_lettered")]
    [InlineData(ConsumeOutcome.Requeued, "requeued")]
    public void Consumed_CountsByQueueAndOutcome(ConsumeOutcome outcome, string expectedTag)
    {
        using var collector = new MetricCollector<long>(MeterFactory, MessagingMetrics.MeterName, "telemetry.messaging.consumed");
        var metrics = new MessagingMetrics(MeterFactory);

        metrics.Consumed("processor.meter-batches", outcome);

        var measurement = collector.LastMeasurement.ShouldNotBeNull();
        measurement.Tags["queue"].ShouldBe("processor.meter-batches");
        measurement.Tags["outcome"].ShouldBe(expectedTag);
    }

    [Fact]
    public void Consumed_WithDuration_RecordsHandlerDuration()
    {
        using var collector = new MetricCollector<double>(MeterFactory, MessagingMetrics.MeterName, "telemetry.messaging.handler.duration");
        var metrics = new MessagingMetrics(MeterFactory);

        metrics.Consumed("q", ConsumeOutcome.Acked, TimeSpan.FromMilliseconds(42));
        metrics.Consumed("q", ConsumeOutcome.DeadLettered);

        var measurement = collector.GetMeasurementSnapshot().ShouldHaveSingleItem();
        measurement.Value.ShouldBe(42);
        measurement.Tags["queue"].ShouldBe("q");
    }

    [Fact]
    public void HandlerRetried_CountsByQueue()
    {
        using var collector = new MetricCollector<long>(MeterFactory, MessagingMetrics.MeterName, "telemetry.messaging.handler.retries");
        var metrics = new MessagingMetrics(MeterFactory);

        metrics.HandlerRetried("q");
        metrics.HandlerRetried("q");

        collector.GetMeasurementSnapshot().Sum(m => m.Value).ShouldBe(2);
    }

    public void Dispose() => _services.Dispose();
}
