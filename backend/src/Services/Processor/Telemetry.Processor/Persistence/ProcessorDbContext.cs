using Microsoft.EntityFrameworkCore;
using Telemetry.Processor.Domain;

namespace Telemetry.Processor.Persistence;

/// <summary>
/// Write model of the telemetry database. This service owns the schema and its migrations; other services
/// only read from it.
/// </summary>
public sealed class ProcessorDbContext(DbContextOptions<ProcessorDbContext> options) : DbContext(options)
{
    public DbSet<Meter> Meters => Set<Meter>();

    public DbSet<MeterBatch> Batches => Set<MeterBatch>();

    public DbSet<Reading> Readings => Set<Reading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProcessorDbContext).Assembly);
}
