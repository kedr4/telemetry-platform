using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Telemetry.Contracts.Readings;
using Telemetry.Processor.Domain;

namespace Telemetry.Processor.Persistence.Configurations;

internal sealed class MeterConfiguration : IEntityTypeConfiguration<Meter>
{
    public const int TypeMaxLength = 32;

    public void Configure(EntityTypeBuilder<Meter> builder)
    {
        builder.ToTable("meters");

        builder.HasKey(meter => meter.Id);
        builder.Property(meter => meter.Id).UseIdentityAlwaysColumn();

        builder.Property(meter => meter.Location)
            .HasMaxLength(Meter.MaxLocationLength)
            .IsRequired();

        // Stored with the same wire names as in the upstream API and messages (energy, air_quality, motion).
        builder.Property(meter => meter.Type)
            .HasConversion(type => type.ToWireName(), name => MeterTypeNames.Parse(name))
            .HasMaxLength(TypeMaxLength)
            .IsRequired();

        builder.HasIndex(meter => new { meter.Location, meter.Type }).IsUnique();
    }
}
