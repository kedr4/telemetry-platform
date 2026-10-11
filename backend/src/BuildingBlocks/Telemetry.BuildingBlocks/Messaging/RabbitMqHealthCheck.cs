using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Telemetry.BuildingBlocks.Messaging;

internal sealed class RabbitMqHealthCheck(IRabbitMqConnection connection) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(connection.IsConnected
            ? HealthCheckResult.Healthy("Connected to RabbitMQ.")
            : HealthCheckResult.Unhealthy("Not connected to RabbitMQ."));
}
