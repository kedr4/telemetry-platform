using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Testcontainers.RabbitMq;
using Xunit;

namespace Telemetry.Testing.RabbitMq;

/// <summary>
/// A disposable RabbitMQ broker. Register with <c>[assembly: AssemblyFixture(typeof(RabbitMqFixture))]</c>
/// to share one container across a test assembly.
/// </summary>
public sealed class RabbitMqFixture : IAsyncLifetime
{
    public const string UserName = "telemetry";
    public const string Password = "telemetry";

    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:4.1-alpine")
        .WithUsername(UserName)
        .WithPassword(Password)
        .Build();

    public string HostName => _container.Hostname;

    public int Port => _container.GetMappedPublicPort(RabbitMqBuilder.RabbitMqPort);

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    /// <summary>
    /// Waits until a service has started consuming <paramref name="queueName"/>. Each probe uses a fresh
    /// channel, because asking about a queue that does not exist yet makes the broker close the channel.
    /// </summary>
    public async Task WaitForConsumerAsync(string queueName, CancellationToken cancellationToken)
    {
        await using var connection = await CreateConnectionAsync(cancellationToken);
        await Eventually.TrueAsync(
            async () =>
            {
                await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
                try
                {
                    return await channel.ConsumerCountAsync(queueName, cancellationToken) > 0;
                }
                catch (OperationInterruptedException)
                {
                    return false;
                }
            },
            cancellationToken);
    }

    public async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = HostName,
            Port = Port,
            UserName = UserName,
            Password = Password,
        };
        return await factory.CreateConnectionAsync(cancellationToken);
    }
}
