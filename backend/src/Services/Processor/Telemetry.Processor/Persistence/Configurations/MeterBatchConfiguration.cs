using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Telemetry.Processor.Domain;

namespace Telemetry.Processor.Persistence.Configurations;

internal sealed class MeterBatchConfiguration : IEntityTypeConfiguration<MeterBatch>
{
    public void Configure(EntityTypeBuilder<MeterBatch> builder)
    {
        builder.ToTable("meter_batches", table =>
            table.HasCheckConstraint("ck_meter_batches_reading_count_positive", "reading_count > 0"));

        builder.HasKey(batch => batch.Id);
        builder.Property(batch => batch.Id).ValueGeneratedNever();

        builder.HasIndex(batch => batch.FetchedAt);
    }
}
