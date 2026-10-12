using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Telemetry.BuildingBlocks.Messaging;
using Telemetry.Contracts.Messaging;
using Telemetry.Testing;
using Telemetry.Testing.RabbitMq;

namespace Telemetry.BuildingBlocks.IntegrationTests.Infrastructure;

/// <summary>
/// A generic host wired exactly like a service: messaging plus one consumer on a test-specific queue.
/// </summary>
internal sealed class MessagingTestHost : IAsyncDisposable
{
    private readonly IHost _host;

    private MessagingTestHost(IHost host, string queueName)
    {
        _host = host;
        QueueName = queueName;
    }

    public string QueueName { get; }

    public IMessagePublisher Publisher => _host.Services.GetRequiredService<IMessagePublisher>();

    public IRabbitMqConnection Connection => _host.Services.GetRequiredService<IRabbitMqConnection>();

    public static async Task<MessagingTestHost> StartAsync(
        RabbitMqFixture rabbit,
        HandlerProbe probe,
        CancellationToken cancellationToken)
    {
        var queueName = $"tests.{Guid.NewGuid():N}";

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RabbitMq:HostName"] = rabbit.HostName,
            ["RabbitMq:Port"] = rabbit.Port.ToString(CultureInfo.InvariantCulture),
            ["RabbitMq:UserName"] = RabbitMqFixture.UserName,
            ["RabbitMq:Password"] = RabbitMqFixture.Password,
            ["RabbitMq:ConsumerRetryCount"] = "2",
            ["RabbitMq:ConsumerRetryDelay"] = "00:00:00.010",
        });
        builder.Services.AddSingleton(probe);
        builder.Services.AddRabbitMqMessaging(builder.Configuration);
        builder.Services.AddMessageConsumer<ReadingsPersisted, ProbeHandler>(queueName);

        var host = builder.Build();
        await host.StartAsync(cancellationToken);

        var testHost = new MessagingTestHost(host, queueName);
        await testHost.WaitForQueueAsync(rabbit, cancellationToken);
        return testHost;
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();

        // Never block on async disposal: the broker connection closes asynchronously.
        if (_host is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            _host.Dispose();
        }
    }

    // Consumers declare their queue in the background; publishing before that would be unroutable.
    private async Task WaitForQueueAsync(RabbitMqFixture rabbit, CancellationToken cancellationToken)
    {
        await using var connection = await rabbit.CreateConnectionAsync(cancellationToken);
        await Eventually.TrueAsync(
            async () =>
            {
                await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
                try
                {
                    await channel.QueueDeclarePassiveAsync(QueueName, cancellationToken);
                    return true;
                }
                catch (RabbitMQ.Client.Exceptions.OperationInterruptedException)
                {
                    return false;
                }
            },
            cancellationToken);
    }
}
