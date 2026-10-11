using Microsoft.Extensions.Hosting;

namespace Telemetry.BuildingBlocks.Messaging;

/// <summary>
/// Opens the broker connection eagerly at start-up so readiness reflects broker availability
/// even before the first message is published.
/// </summary>
internal sealed class RabbitMqConnectionInitializer(IRabbitMqConnection connection) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => connection.GetAsync(stoppingToken);
}
