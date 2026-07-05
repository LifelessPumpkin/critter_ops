using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skipper_api.Domain.Animals;

namespace skipper_api.Data.Configurations;

/// <summary>
/// EF Core table mapping and column configuration for the Animal entity.
/// </summary>
public class AnimalConfiguration : IEntityTypeConfiguration<Animal>
{
    public void Configure(EntityTypeBuilder<Animal> builder)
    {
        builder.ToTable("Animals");

        // Primary key
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();

        // Required bounded-string fields
        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Species)
            .IsRequired()
            .HasMaxLength(100);

        // Enum fields - stored as strings for readability in the database
        builder.Property(a => a.AnimalType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.Sex)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // Optional bounded-string fields
        builder.Property(a => a.SubspeciesOrMorph)
            .HasMaxLength(100);

        builder.Property(a => a.DispositionReason)
            .HasMaxLength(100);

        builder.Property(a => a.MicrochipNumber)
            .HasMaxLength(50);

        builder.Property(a => a.TagIdentifier)
            .HasMaxLength(50);

        builder.Property(a => a.Source)
            .HasMaxLength(100);

        // Notes - stored as PostgreSQL text (unbounded)
        builder.Property(a => a.Notes)
            .HasColumnType("text");

        // Date-only fields
        builder.Property(a => a.BirthDate)
            .HasColumnType("date");

        builder.Property(a => a.AcquiredDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(a => a.DispositionDate)
            .HasColumnType("date");

        builder.Property(a => a.BirthDateIsEstimated)
            .IsRequired()
            .HasDefaultValue(false);

        // Timestamps
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();

        // Relationships
        builder.HasOne(a => a.Enclosure)
            .WithMany(e => e.Animals)
            .HasForeignKey(a => a.EnclosureId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Indexes - supports common filtering and lookup queries
        builder.HasIndex(a => a.Name).HasDatabaseName("IX_Animals_Name");
        builder.HasIndex(a => a.Species).HasDatabaseName("IX_Animals_Species");
        builder.HasIndex(a => a.AnimalType).HasDatabaseName("IX_Animals_AnimalType");
        builder.HasIndex(a => a.Status).HasDatabaseName("IX_Animals_Status");
        builder.HasIndex(a => a.EnclosureId).HasDatabaseName("IX_Animals_EnclosureId");
        builder.HasIndex(a => a.MicrochipNumber).HasDatabaseName("IX_Animals_MicrochipNumber");
        builder.HasIndex(a => a.TagIdentifier).HasDatabaseName("IX_Animals_TagIdentifier");
    }
}
