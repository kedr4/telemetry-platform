using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Telemetry.Processor.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> create the context without starting the service (no broker or configuration needed).
/// Migrations are generated offline, so the connection string is never used to connect.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ProcessorDbContext>
{
    public ProcessorDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ProcessorDbContext>();
        PersistenceServiceCollectionExtensions.Configure(options, "Host=localhost;Database=telemetry");
        return new ProcessorDbContext(options.Options);
    }
}
