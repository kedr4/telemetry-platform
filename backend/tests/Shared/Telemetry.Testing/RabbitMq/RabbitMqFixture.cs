using RabbitMQ.Client;
using Telemetry.BuildingBlocks.IntegrationTests.Infrastructure;
using Testcontainers.RabbitMq;

[assembly: AssemblyFixture(typeof(RabbitMqFixture))]

namespace Telemetry.BuildingBlocks.IntegrationTests.Infrastructure;

/// <summary>One RabbitMQ container shared by every test in the assembly.</summary>
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
