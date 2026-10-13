using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Telemetry.BuildingBlocks.Correlation;
using Telemetry.BuildingBlocks.IntegrationTests.Infrastructure;
using Telemetry.BuildingBlocks.Messaging;
using Telemetry.Contracts.Messaging;
using Telemetry.Contracts.Readings;
using Telemetry.Testing;
using Telemetry.Testing.RabbitMq;

namespace Telemetry.BuildingBlocks.IntegrationTests.Messaging;

public sealed class RabbitMqMessagingTests(RabbitMqFixture rabbit)
{
    private static readonly ReadingsPersisted Message = new(
        Guid.NewGuid(),
        DateTimeOffset.UtcNow,
        [new EnergyReading("Kitchen", 604.88), new MotionReading("Office", MotionDetected: false)]);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task PublishedMessage_IsHandled_WithCorrelationId()
    {
        var probe = new HandlerProbe();
        await using var host = await MessagingTestHost.StartAsync(rabbit, probe, CancellationToken);

        using (CorrelationContext.BeginScope("integration-test-1"))
        {
            await host.Publisher.PublishAsync(Message, CancellationToken);
        }

        await Eventually.TrueAsync(() => !probe.Handled.IsEmpty, CancellationToken);
        var (handled, context) = probe.Handled.Single();
        handled.BatchId.ShouldBe(Message.BatchId);
        handled.Readings.ShouldBe(Message.Readings);
        context.CorrelationId.ShouldBe("integration-test-1");
        context.MessageId.ShouldNotBeNullOrEmpty();
        host.Connection.IsConnected.ShouldBeTrue();
    }

    [Fact]
    public async Task FailingHandler_IsRetried_ThenMessageIsDeadLettered()
    {
        var probe = new HandlerProbe { Failure = () => new InvalidOperationException("Database unavailable") };
        await using var host = await MessagingTestHost.StartAsync(rabbit, probe, CancellationToken);

        await host.Publisher.PublishAsync(Message, CancellationToken);

        await Eventually.TrueAsync(() => DeadLetterCountAsync(host.QueueName, expected: 1), CancellationToken);
        probe.Attempts.ShouldBe(3); // first attempt + 2 configured retries
        probe.Handled.ShouldBeEmpty();
    }

    [Fact]
    public async Task NonRetryableFailure_IsDeadLetteredWithoutRetry()
    {
        var probe = new HandlerProbe { Failure = () => new NonRetryableMessageException("Invalid batch") };
        await using var host = await MessagingTestHost.StartAsync(rabbit, probe, CancellationToken);

        await host.Publisher.PublishAsync(Message, CancellationToken);

        await Eventually.TrueAsync(() => DeadLetterCountAsync(host.QueueName, expected: 1), CancellationToken);
        probe.Attempts.ShouldBe(1);
    }

    [Fact]
    public async Task MalformedMessage_IsDeadLetteredWithoutReachingHandler()
    {
        var probe = new HandlerProbe();
        await using var host = await MessagingTestHost.StartAsync(rabbit, probe, CancellationToken);

        await PublishRawToQueueAsync(host.QueueName, "{ definitely not a message");

        await Eventually.TrueAsync(() => DeadLetterCountAsync(host.QueueName, expected: 1), CancellationToken);
        probe.Attempts.ShouldBe(0);
    }

    [Fact]
    public async Task WorkItemWithoutSubscriber_FailsToPublish()
    {
        await using var host = await MessagingTestHost.StartAsync(rabbit, new HandlerProbe(), CancellationToken);

        await Should.ThrowAsync<PublishReturnException>(() => host.Publisher.PublishAsync(new UnroutedWorkItem(1), CancellationToken));
    }

    [Fact]
    public async Task EventWithoutSubscriber_IsPublished()
    {
        await using var host = await MessagingTestHost.StartAsync(rabbit, new HandlerProbe(), CancellationToken);

        await Should.NotThrowAsync(() => host.Publisher.PublishAsync(new UnroutedEvent(1), CancellationToken));

        // The publisher stays usable afterwards.
        await Should.ThrowAsync<PublishReturnException>(() => host.Publisher.PublishAsync(new UnroutedWorkItem(2), CancellationToken));
    }

    private async Task<bool> DeadLetterCountAsync(string queueName, uint expected)
    {
        await using var connection = await rabbit.CreateConnectionAsync(CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: CancellationToken);
        var count = await channel.MessageCountAsync(MessagingTopology.DeadLetterQueueFor(queueName), CancellationToken);
        return count == expected;
    }

    private async Task PublishRawToQueueAsync(string queueName, string body)
    {
        await using var connection = await rabbit.CreateConnectionAsync(CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: CancellationToken);
        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            body: Encoding.UTF8.GetBytes(body),
            cancellationToken: CancellationToken);
    }
}
