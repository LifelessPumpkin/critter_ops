using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skipper_api.Domain.Enclosures;

namespace skipper_api.Data.Configurations;

/// <summary>
/// EF Core table mapping and column configuration for the Enclosure entity.
/// </summary>
public class EnclosureConfiguration : IEntityTypeConfiguration<Enclosure>
{
    public void Configure(EntityTypeBuilder<Enclosure> builder)
    {
        builder.ToTable("Enclosures");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Required bounded-string fields
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.Location)
            .IsRequired()
            .HasMaxLength(150);

        // Enum fields — stored as strings for readability in the database
        builder.Property(e => e.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Mobility)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // Optional bounded-string fields
        builder.Property(e => e.SizeLabel)
            .HasMaxLength(100);

        builder.Property(e => e.DimensionUnit)
            .HasMaxLength(30);

        builder.Property(e => e.VolumeUnit)
            .HasMaxLength(30);

        builder.Property(e => e.Material)
            .HasMaxLength(100);

        builder.Property(e => e.SafetyRating)
            .HasMaxLength(50);

        // Decimal precision — (10, 4) provides enough range and resolution for
        // physical dimensions (inches/cm/m) and volume (gallons/liters)
        builder.Property(e => e.Length)
            .HasPrecision(10, 4);

        builder.Property(e => e.Width)
            .HasPrecision(10, 4);

        builder.Property(e => e.Height)
            .HasPrecision(10, 4);

        builder.Property(e => e.Volume)
            .HasPrecision(12, 4);

        // Notes — stored as PostgreSQL text (unbounded)
        builder.Property(e => e.Notes)
            .HasColumnType("text");

        // Timestamps
        builder.Property(e => e.CreatedDate).IsRequired();
        builder.Property(e => e.UpdatedDate).IsRequired();

        // Indexes — supports common filtering and reporting queries
        builder.HasIndex(e => e.Name).HasDatabaseName("IX_Enclosures_Name");
        builder.HasIndex(e => e.Type).HasDatabaseName("IX_Enclosures_Type");
        builder.HasIndex(e => e.Status).HasDatabaseName("IX_Enclosures_Status");
        builder.HasIndex(e => e.Location).HasDatabaseName("IX_Enclosures_Location");
    }
}
