using Microsoft.EntityFrameworkCore;
using Telemetry.Processor.Persistence;
using Telemetry.Testing.PostgreSql;

namespace Telemetry.Processor.IntegrationTests.Infrastructure;

/// <summary>
/// A PostgreSQL container with the real migrations applied, shared by the whole assembly.
/// Tests stay independent by using unique locations and batch ids.
/// </summary>
public sealed class MigratedDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlFixture _postgres = new();

    public string ConnectionString => _postgres.ConnectionString;

    public async ValueTask InitializeAsync()
    {
        await _postgres.InitializeAsync();
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public ValueTask DisposeAsync() => _postgres.DisposeAsync();

    public ProcessorDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ProcessorDbContext>();
        PersistenceServiceCollectionExtensions.Configure(options, ConnectionString);
        return new ProcessorDbContext(options.Options);
    }
}
