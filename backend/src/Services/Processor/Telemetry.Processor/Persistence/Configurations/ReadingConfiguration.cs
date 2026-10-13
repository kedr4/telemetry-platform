using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Telemetry.Processor.Domain;

namespace Telemetry.Processor.Persistence.Configurations;

internal sealed class ReadingConfiguration : IEntityTypeConfiguration<Reading>
{
    // Exactly one measurement group per row: energy, air quality (all three values) or motion.
    private const string SingleMeasurementSql =
        "num_nonnulls(energy, co2, motion_detected) = 1 "
        + "AND (co2 IS NULL) = (pm25 IS NULL) AND (co2 IS NULL) = (humidity IS NULL)";

    public void Configure(EntityTypeBuilder<Reading> builder)
    {
        builder.ToTable("readings", table =>
        {
            table.HasCheckConstraint("ck_readings_single_measurement", SingleMeasurementSql);
            table.HasCheckConstraint("ck_readings_energy_non_negative", "energy >= 0");
            table.HasCheckConstraint("ck_readings_humidity_percentage", "humidity BETWEEN 0 AND 100");
            table.HasCheckConstraint("ck_readings_air_quality_non_negative", "co2 >= 0 AND pm25 >= 0");
        });

        builder.HasKey(reading => reading.Id);
        builder.Property(reading => reading.Id).UseIdentityAlwaysColumn();

        builder.HasOne(reading => reading.Meter)
            .WithMany()
            .HasForeignKey(reading => reading.MeterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<MeterBatch>()
            .WithMany()
            .HasForeignKey(reading => reading.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        // Idempotency: a meter appears at most once per batch.
        builder.HasIndex(reading => new { reading.BatchId, reading.MeterId }).IsUnique();

        // Serves "latest value per meter" and per-meter time series.
        builder.HasIndex(reading => new { reading.MeterId, reading.RecordedAt }).IsDescending(false, true);

        // Serves time-range filters and time-bucket aggregations across meters.
        builder.HasIndex(reading => reading.RecordedAt);
    }
}
