using RabbitMQ.Client;

namespace Telemetry.BuildingBlocks.Messaging;

/// <summary>
/// Broker topology: a durable topic exchange for integration messages and a direct dead-letter
/// exchange with one dead-letter queue per consumer queue. All declarations are idempotent.
/// </summary>
public static class MessagingTopology
{
    public const string Exchange = "telemetry.events";
    public const string DeadLetterExchange = "telemetry.dead-letter";

    public static string DeadLetterQueueFor(string queueName) => $"{queueName}.dlq";

    public static async Task DeclareExchangesAsync(IChannel channel, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);

        await channel.ExchangeDeclareAsync(
            exchange: Exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: DeadLetterExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
    }

    public static async Task DeclareConsumerQueueAsync(
        IChannel channel,
        string queueName,
        string routingKey,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);

        await DeclareExchangesAsync(channel, cancellationToken);

        var deadLetterQueue = DeadLetterQueueFor(queueName);
        await channel.QueueDeclareAsync(
            queue: deadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            queue: deadLetterQueue,
            exchange: DeadLetterExchange,
            routingKey: queueName,
            cancellationToken: cancellationToken);

        var arguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = DeadLetterExchange,
            ["x-dead-letter-routing-key"] = queueName,
        };
        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: arguments,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            queue: queueName,
            exchange: Exchange,
            routingKey: routingKey,
            cancellationToken: cancellationToken);
    }
}
