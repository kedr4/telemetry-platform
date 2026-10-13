using Testcontainers.PostgreSql;
using Xunit;

namespace Telemetry.Testing.PostgreSql;

/// <summary>
/// A disposable PostgreSQL server. Register with <c>[assembly: AssemblyFixture(typeof(PostgreSqlFixture))]</c>
/// to share one container across a test assembly; tests isolate themselves through unique data.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("telemetry")
        .WithUsername("telemetry")
        .WithPassword("telemetry")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
