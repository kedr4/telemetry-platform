using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Telemetry.BuildingBlocks.Messaging;
using Telemetry.Contracts.Messaging;
using Telemetry.Processor.IntegrationTests.Infrastructure;
using Telemetry.Testing;
using Telemetry.Testing.Metrics;
using Telemetry.Testing.RabbitMq;

namespace Telemetry.Processor.IntegrationTests;

/// <summary>
/// Runs the real Processor application against PostgreSQL and RabbitMQ containers.
/// </summary>
public sealed class ProcessorApplicationTests(MigratedDatabaseFixture database, RabbitMqFixture rabbit)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task BatchFromTheBroker_IsStored_AndAnnounced_AndPoisonIsDeadLettered_AndMetricsAreExported()
    {
        await using var factory = CreateFactory();
        using var http = factory.CreateClient();
        await using var connection = await rabbit.CreateConnectionAsync(CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: CancellationToken);

        var announcements = await DeclareAnnouncementQueueAsync(channel);
        await rabbit.WaitForConsumerAsync(QueueNames.ProcessorMeterBatches, CancellationToken);

        // A valid batch is stored and announced.
        var message = Batches.ForLocation(Batches.UniqueLocation("Kitchen"));
        await PublishAsync(channel, MeterBatchFetched.RoutingKey, MessageSerializer.Serialize(message));

        await Eventually.TrueAsync(
            async () =>
            {
                await using var db = database.CreateDbContext();
                return await db.Readings.CountAsync(r => r.BatchId == message.BatchId, CancellationToken) == 3;
            },
            CancellationToken);

        BasicGetResult? announcement = null;
        await Eventually.TrueAsync(
            async () =>
            {
                announcement = await channel.BasicGetAsync(announcements, autoAck: true, CancellationToken);
                return announcement is not null;
            },
            CancellationToken);
        MessageSerializer.Deserialize<ReadingsPersisted>(announcement!.Body).BatchId.ShouldBe(message.BatchId);

        // A malformed batch ends up in the dead-letter queue instead of blocking the queue.
        await PublishAsync(channel, MeterBatchFetched.RoutingKey, Encoding.UTF8.GetBytes("{ not a batch"));
        var deadLetterQueue = MessagingTopology.DeadLetterQueueFor(QueueNames.ProcessorMeterBatches);
        await Eventually.TrueAsync(
            async () => await channel.MessageCountAsync(deadLetterQueue, CancellationToken) >= 1,
            CancellationToken);

        var readiness = await http.GetAsync(new Uri("/health/ready", UriKind.Relative), CancellationToken);
        readiness.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Both outcomes are visible to Prometheus.
        var metrics = await http.GetStringAsync(new Uri("/metrics", UriKind.Relative), CancellationToken);
        PrometheusText.HasSample(metrics, "telemetry_processor_batches_total", ("outcome", "stored")).ShouldBeTrue();
        PrometheusText.HasSample(metrics, "telemetry_processor_readings_stored_total").ShouldBeTrue();
        PrometheusText.HasSample(
            metrics,
            "telemetry_messaging_consumed_total",
            ("queue", QueueNames.ProcessorMeterBatches),
            ("outcome", "acked")).ShouldBeTrue();
        PrometheusText.HasSample(
            metrics,
            "telemetry_messaging_consumed_total",
            ("queue", QueueNames.ProcessorMeterBatches),
            ("outcome", "dead_lettered")).ShouldBeTrue();
        PrometheusText.HasSample(
            metrics,
            "telemetry_messaging_published_total",
            ("message_type", nameof(ReadingsPersisted)),
            ("outcome", "confirmed")).ShouldBeTrue();
    }

    private static async Task PublishAsync(IChannel channel, string routingKey, byte[] body) =>
        await channel.BasicPublishAsync(MessagingTopology.Exchange, routingKey, body, CancellationToken);

    private static async Task<string> DeclareAnnouncementQueueAsync(IChannel channel)
    {
        var queue = $"tests.announcements.{Guid.NewGuid():N}";
        await MessagingTopology.DeclareConsumerQueueAsync(channel, queue, ReadingsPersisted.RoutingKey, CancellationToken);
        return queue;
    }

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("ConnectionStrings:Telemetry", database.ConnectionString)
            .UseSetting("RabbitMq:HostName", rabbit.HostName)
            .UseSetting("RabbitMq:Port", rabbit.Port.ToString(CultureInfo.InvariantCulture))
            .UseSetting("RabbitMq:UserName", RabbitMqFixture.UserName)
            .UseSetting("RabbitMq:Password", RabbitMqFixture.Password));
}
