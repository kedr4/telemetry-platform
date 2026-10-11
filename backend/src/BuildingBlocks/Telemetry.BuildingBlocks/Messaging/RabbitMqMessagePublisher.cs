using System.Net.Mime;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Telemetry.BuildingBlocks.Correlation;
using Telemetry.Contracts.Messaging;

namespace Telemetry.BuildingBlocks.Messaging;

/// <summary>
/// Publishes over a single channel with publisher confirms. Messages are published as mandatory, so
/// a message that no queue is bound for fails instead of being silently dropped.
/// </summary>
internal sealed partial class RabbitMqMessagePublisher(
    IRabbitMqConnection connection,
    TimeProvider timeProvider,
    ILogger<RabbitMqMessagePublisher> logger) : IMessagePublisher, IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IChannel? _channel;

    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : IIntegrationMessage
    {
        ArgumentNullException.ThrowIfNull(message);

        var body = MessageSerializer.Serialize(message);
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = MediaTypeNames.Application.Json,
            MessageId = Guid.NewGuid().ToString("N"),
            Type = typeof(TMessage).Name,
            CorrelationId = CorrelationContext.CorrelationId ?? CorrelationContext.NewId(),
            Timestamp = new AmqpTimestamp(timeProvider.GetUtcNow().ToUnixTimeSeconds()),
        };

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var channel = await GetChannelAsync(cancellationToken);
            await channel.BasicPublishAsync(
                exchange: MessagingTopology.Exchange,
                routingKey: TMessage.RoutingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }
        catch
        {
            // A failed publish may leave the channel unusable; start fresh next time.
            await ResetChannelAsync();
            throw;
        }
        finally
        {
            _lock.Release();
        }

        LogPublished(properties.Type, TMessage.RoutingKey, properties.MessageId);
    }

    public async ValueTask DisposeAsync()
    {
        await ResetChannelAsync();
        _lock.Dispose();
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        await ResetChannelAsync();

        var brokerConnection = await connection.GetAsync(cancellationToken);
        var channel = await brokerConnection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            cancellationToken);
        await MessagingTopology.DeclareExchangesAsync(channel, cancellationToken);

        _channel = channel;
        return channel;
    }

    private async Task ResetChannelAsync()
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            await _channel.DisposeAsync();
        }
        catch (Exception ex)
        {
            LogChannelDisposeFailed(ex);
        }

        _channel = null;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Published {MessageType} with routing key {RoutingKey} ({MessageId})")]
    private partial void LogPublished(string messageType, string routingKey, string messageId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Disposing a broken publisher channel failed")]
    private partial void LogChannelDisposeFailed(Exception exception);
}
