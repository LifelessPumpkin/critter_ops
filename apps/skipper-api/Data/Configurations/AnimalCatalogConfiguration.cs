using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skipper_api.Domain.Animals;

namespace skipper_api.Data.Configurations;

public class AnimalGroupConfiguration : IEntityTypeConfiguration<AnimalGroup>
{
    public void Configure(EntityTypeBuilder<AnimalGroup> b)
    {
        b.ToTable("AnimalGroups");
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();
        b.Property(x => x.LegacyAnimalType).HasConversion<string>().HasMaxLength(50);
        b.HasData(AnimalCatalogSeed.Groups);
    }
}
public class SpeciesConfiguration : IEntityTypeConfiguration<Species>
{
    public void Configure(EntityTypeBuilder<Species> b)
    {
        b.ToTable("Species", t => t.HasCheckConstraint("CK_Species_ScientificName", "length(trim(\"ScientificName\")) > 0"));
        b.Property(x => x.CommonName).HasMaxLength(100);
        b.Property(x => x.ScientificName).HasMaxLength(100).IsRequired();
        b.Property(x => x.PreferredVariantType).HasConversion<string>().HasMaxLength(30);
        b.HasOne(x => x.AnimalGroup).WithMany().HasForeignKey(x => x.AnimalGroupId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.AnimalGroupId, x.ScientificName, x.Id });
        b.HasIndex(x => x.CommonName).HasMethod("gin").HasOperators("gin_trgm_ops");
        b.HasIndex(x => x.ScientificName).HasMethod("gin").HasOperators("gin_trgm_ops");
        b.HasData(AnimalCatalogSeed.Species);
    }
}
public class AnimalVariantConfiguration : IEntityTypeConfiguration<AnimalVariant>
{
    public void Configure(EntityTypeBuilder<AnimalVariant> b)
    {
        b.ToTable("AnimalVariants");
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.VariantType).HasConversion<string>().HasMaxLength(30);
        b.HasOne(x => x.Species).WithMany().HasForeignKey(x => x.SpeciesId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SpeciesId, x.VariantType, x.Name }).IsUnique();
        b.HasIndex(x => x.Name).HasMethod("gin").HasOperators("gin_trgm_ops");
        b.HasData(AnimalCatalogSeed.Variants);
    }
}
