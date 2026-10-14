using Microsoft.EntityFrameworkCore;

namespace Telemetry.Gateway.Infrastructure.Persistence;

/// <summary>
/// Read-only view of the telemetry database. The schema is owned and migrated by the Processor;
/// this context maps only what the gateway reads and refuses to write.
/// </summary>
internal sealed class TelemetryReadDbContext(DbContextOptions<TelemetryReadDbContext> options) : DbContext(options)
{
    public DbSet<MeterRow> Meters => Set<MeterRow>();

    public DbSet<ReadingRow> Readings => Set<ReadingRow>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new InvalidOperationException("The gateway database context is read-only.");

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("The gateway database context is read-only.");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MeterRow>(meter =>
        {
            meter.ToTable("meters", table => table.ExcludeFromMigrations());
            meter.HasKey(row => row.Id);
        });

        modelBuilder.Entity<ReadingRow>(reading =>
        {
            reading.ToTable("readings", table => table.ExcludeFromMigrations());
            reading.HasKey(row => row.Id);
            reading.HasOne(row => row.Meter).WithMany().HasForeignKey(row => row.MeterId);
        });
    }
}
