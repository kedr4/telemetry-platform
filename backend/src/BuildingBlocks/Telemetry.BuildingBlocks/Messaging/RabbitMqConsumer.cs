using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Telemetry.BuildingBlocks.Correlation;
using Telemetry.Contracts.Messaging;

namespace Telemetry.BuildingBlocks.Messaging;

/// <summary>
/// Consumes one queue and dispatches each message to its <see cref="IMessageHandler{TMessage}"/>.
/// <list type="bullet">
/// <item>Success → ack.</item>
/// <item>Transient failure → retried in-process with exponential back-off.</item>
/// <item>Undeserializable message, <see cref="NonRetryableMessageException"/> or retries exhausted → dead-lettered.</item>
/// <item>Shutdown while handling → requeued.</item>
/// </list>
/// </summary>
internal sealed partial class RabbitMqConsumer<TMessage> : BackgroundService
    where TMessage : IIntegrationMessage
{
    private readonly string _queueName;
    private readonly IRabbitMqConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqConsumer<TMessage>> _logger;
    private readonly ResiliencePipeline _handlerPipeline;
    private IChannel? _channel;

    public RabbitMqConsumer(
        string queueName,
        IRabbitMqConnection connection,
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqConsumer<TMessage>> logger)
    {
        _queueName = queueName;
        _connection = connection;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
        _handlerPipeline = BuildHandlerPipeline();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
            _channel = null;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await _connection.GetAsync(stoppingToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        _channel = channel;

        await MessagingTopology.DeclareConsumerQueueAsync(channel, _queueName, TMessage.RoutingKey, stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: _options.PrefetchCount, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => OnReceivedAsync(channel, delivery, stoppingToken);
        await channel.BasicConsumeAsync(_queueName, autoAck: false, consumer, stoppingToken);

        LogConsuming(_queueName, typeof(TMessage).Name);
    }

    private async Task OnReceivedAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        var correlationId = CorrelationContext.IsValid(delivery.BasicProperties.CorrelationId)
            ? delivery.BasicProperties.CorrelationId!
            : CorrelationContext.NewId();

        using var correlationScope = CorrelationContext.BeginScope(correlationId);

        TMessage message;
        try
        {
            message = MessageSerializer.Deserialize<TMessage>(delivery.Body);
        }
        catch (JsonException ex)
        {
            LogUndeserializable(ex, _queueName, delivery.BasicProperties.MessageId);
            await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, CancellationToken.None);
            return;
        }

        var context = new MessageContext(delivery.BasicProperties.MessageId, correlationId, delivery.Redelivered);

        try
        {
            await _handlerPipeline.ExecuteAsync(
                async token => await HandleInScopeAsync(message, context, token),
                stoppingToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, CancellationToken.None);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            LogDeadLettered(ex, _queueName, context.MessageId);
            await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, CancellationToken.None);
        }
    }

    private async Task HandleInScopeAsync(TMessage message, MessageContext context, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<TMessage>>();
        await handler.HandleAsync(message, context, cancellationToken);
    }

    private ResiliencePipeline BuildHandlerPipeline()
    {
        if (_options.ConsumerRetryCount == 0)
        {
            return ResiliencePipeline.Empty;
        }

        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(
                    ex => ex is not NonRetryableMessageException and not OperationCanceledException),
                MaxRetryAttempts = _options.ConsumerRetryCount,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = _options.ConsumerRetryDelay,
                OnRetry = args =>
                {
                    LogHandlerRetry(args.Outcome.Exception, _queueName, args.AttemptNumber + 1);
                    return ValueTask.CompletedTask;
                },
            })
            .Build();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Consuming {MessageType} messages from queue {Queue}")]
    private partial void LogConsuming(string queue, string messageType);

    [LoggerMessage(Level = LogLevel.Error, Message = "Message {MessageId} on queue {Queue} could not be deserialized; dead-lettering it")]
    private partial void LogUndeserializable(Exception exception, string queue, string? messageId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Handling a message from queue {Queue} failed; retry {Attempt}")]
    private partial void LogHandlerRetry(Exception? exception, string queue, int attempt);

    [LoggerMessage(Level = LogLevel.Error, Message = "Message {MessageId} on queue {Queue} could not be handled; dead-lettering it")]
    private partial void LogDeadLettered(Exception exception, string queue, string? messageId);
}
