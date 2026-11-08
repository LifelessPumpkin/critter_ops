using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skipper_api.Domain.Animals;

namespace skipper_api.Data.Configurations;

public class AnimalVariantAssignmentConfiguration : IEntityTypeConfiguration<AnimalVariantAssignment>
{
    public void Configure(EntityTypeBuilder<AnimalVariantAssignment> b)
    {
        b.ToTable("AnimalVariantAssignments", t => {
            t.HasCheckConstraint("CK_Assignment_Value", "(\"AnimalVariantId\" IS NOT NULL AND \"CustomValue\" IS NULL AND \"VariantType\" IS NULL) OR (\"AnimalVariantId\" IS NULL AND \"NormalizedCustomValue\" <> '' AND \"CustomValue\" IS NOT NULL AND \"VariantType\" IS NOT NULL)");
            t.HasCheckConstraint("CK_Assignment_Type", "\"VariantType\" IS NULL OR \"VariantType\" IN ('Breed','Morph','Variety','Strain','Mutation','Subspecies','Other')");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.CustomValue).HasMaxLength(100);
        b.Property(x => x.VariantType).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.NormalizedCustomValue).HasComputedColumnSql("lower(trim(regexp_replace(\"CustomValue\", '[[:space:]]+', ' ', 'g')))", stored: true);
        b.HasOne(x => x.Animal).WithMany(x => x.VariantAssignments).HasForeignKey(x => x.AnimalId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.AnimalVariant).WithMany().HasForeignKey(x => x.AnimalVariantId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.AnimalId, x.AnimalVariantId }).IsUnique().HasFilter("\"AnimalVariantId\" IS NOT NULL");
        b.HasIndex(x => new { x.AnimalId, x.VariantType, x.NormalizedCustomValue }).IsUnique().HasFilter("\"AnimalVariantId\" IS NULL");
    }
}
