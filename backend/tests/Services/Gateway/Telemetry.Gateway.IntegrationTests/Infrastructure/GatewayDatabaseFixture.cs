extern alias processor;

using Microsoft.EntityFrameworkCore;
using processor::Telemetry.Processor.Persistence;
using Telemetry.Testing.PostgreSql;

namespace Telemetry.Gateway.IntegrationTests.Infrastructure;

/// <summary>
/// PostgreSQL with the schema created by the Processor's real migrations, so the tests prove the gateway's
/// read model matches what the Processor writes.
/// </summary>
public sealed class GatewayDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlFixture _postgres = new();

    public string ConnectionString => _postgres.ConnectionString;

    public async ValueTask InitializeAsync()
    {
        await _postgres.InitializeAsync();

        var options = new DbContextOptionsBuilder<ProcessorDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var db = new ProcessorDbContext(options);
        await db.Database.MigrateAsync();
    }

    public ValueTask DisposeAsync() => _postgres.DisposeAsync();
}
