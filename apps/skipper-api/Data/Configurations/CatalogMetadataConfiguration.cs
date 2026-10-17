using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skipper_api.Domain.Animals;

namespace skipper_api.Data.Configurations;

internal static class CatalogMapping
{
    internal static void Identity<T>(EntityTypeBuilder<T> b, string table) where T : class
    {
        b.ToTable(table);
        b.HasKey("Id");
        b.Property<int>("Id").ValueGeneratedOnAdd();
    }

    internal static void Provider<T>(EntityTypeBuilder<T> b, string table) where T : class
    {
        b.Property<string>("Provider").HasMaxLength(100).IsRequired();
        b.Property<string>("DatasetIdentifier").HasMaxLength(300);
        // A canonical, extensible slug shared by references, runs and standalone issues.
        b.ToTable(table, t => {
            t.HasCheckConstraint($"CK_{table}_Provider", "\"Provider\" ~ '^[a-z0-9][a-z0-9._-]*$'");
            t.HasCheckConstraint($"CK_{table}_Dataset", "\"DatasetIdentifier\" IS NULL OR (\"DatasetIdentifier\" = btrim(\"DatasetIdentifier\") AND length(\"DatasetIdentifier\") > 0)");
        });
    }

    internal static void Name<T>(EntityTypeBuilder<T> b, string table) where T : class
    {
        b.Property<string>("Name").HasMaxLength(300).IsRequired();
        b.Property<string>("Source").HasMaxLength(300);
        b.Property<DateTime>("CreatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");
        b.Property<string>("NameKey").HasComputedColumnSql("lower(btrim(\"Name\"))", stored: true);
        b.Property<string>("SourceKey").HasComputedColumnSql("nullif(lower(btrim(\"Source\")), '')", stored: true);
        b.ToTable(table, t => t.HasCheckConstraint($"CK_{table}_Name", "length(btrim(\"Name\")) > 0"));
        b.HasIndex("NameKey");
    }
}

public class SpeciesCommonNameConfiguration : IEntityTypeConfiguration<SpeciesCommonName>
{
    public void Configure(EntityTypeBuilder<SpeciesCommonName> b)
    {
        CatalogMapping.Identity(b, "SpeciesCommonNames");
        CatalogMapping.Name(b, "SpeciesCommonNames");
        b.Property(x => x.LanguageCode).HasMaxLength(35);
        b.Property<string>("LanguageKey").HasComputedColumnSql("nullif(lower(btrim(\"LanguageCode\")), '')", stored: true);
        b.HasIndex("SpeciesId", "NameKey", "LanguageKey", "SourceKey").IsUnique().AreNullsDistinct(false);
        b.HasOne(x => x.Species).WithMany(x => x.CommonNames).HasForeignKey(x => x.SpeciesId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SpeciesAliasConfiguration : IEntityTypeConfiguration<SpeciesAlias>
{
    public void Configure(EntityTypeBuilder<SpeciesAlias> b)
    {
        CatalogMapping.Identity(b, "SpeciesAliases");
        CatalogMapping.Name(b, "SpeciesAliases");
        b.Property(x => x.AliasType).HasConversion<string>().HasMaxLength(30);
        b.HasIndex("SpeciesId", "NameKey", "AliasType", "SourceKey").IsUnique().AreNullsDistinct(false);
        b.HasOne(x => x.Species).WithMany(x => x.Aliases).HasForeignKey(x => x.SpeciesId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SpeciesExternalReferenceConfiguration : IEntityTypeConfiguration<SpeciesExternalReference>
{
    public void Configure(EntityTypeBuilder<SpeciesExternalReference> b)
    {
        CatalogMapping.Identity(b, "SpeciesExternalReferences");
        CatalogMapping.Provider(b, "SpeciesExternalReferences");
        b.Property(x => x.ExternalIdentifier).HasMaxLength(300).IsRequired();
        b.Property(x => x.ExternalAcceptedIdentifier).HasMaxLength(300);
        b.Property(x => x.ExternalScientificName).HasMaxLength(500);
        b.Property(x => x.ExternalTaxonomicStatus).HasMaxLength(200);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        b.ToTable("SpeciesExternalReferences", t => t.HasCheckConstraint("CK_SpeciesExternalReferences_Identifier", "\"ExternalIdentifier\" = btrim(\"ExternalIdentifier\") AND length(\"ExternalIdentifier\") > 0"));
        b.HasIndex(x => new { x.Provider, x.DatasetIdentifier, x.ExternalIdentifier }).IsUnique().AreNullsDistinct(false);
        b.HasOne(x => x.Species).WithMany(x => x.ExternalReferences).HasForeignKey(x => x.SpeciesId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CatalogSyncRunConfiguration : IEntityTypeConfiguration<CatalogSyncRun>
{
    public void Configure(EntityTypeBuilder<CatalogSyncRun> b)
    {
        CatalogMapping.Identity(b, "CatalogSyncRuns");
        CatalogMapping.Provider(b, "CatalogSyncRuns");
        b.Property(x => x.OperationType).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.ErrorMessage).HasMaxLength(4000);
        b.HasIndex(x => new { x.Provider, x.StartedAt });
        b.HasIndex(x => new { x.Status, x.StartedAt });
        b.ToTable("CatalogSyncRuns", t => {
            t.HasCheckConstraint("CK_CatalogSyncRuns_Counts", "\"RecordsProcessed\" >= 0 AND \"RecordsCreated\" >= 0 AND \"RecordsUpdated\" >= 0 AND \"RecordsSkipped\" >= 0 AND \"RecordsFailed\" >= 0");
            t.HasCheckConstraint("CK_CatalogSyncRuns_Times", "\"CompletedAt\" IS NULL OR (\"StartedAt\" IS NOT NULL AND \"CompletedAt\" >= \"StartedAt\")");
        });
    }
}

public class CatalogSyncIssueConfiguration : IEntityTypeConfiguration<CatalogSyncIssue>
{
    public void Configure(EntityTypeBuilder<CatalogSyncIssue> b)
    {
        CatalogMapping.Identity(b, "CatalogSyncIssues");
        CatalogMapping.Provider(b, "CatalogSyncIssues");
        b.Property(x => x.ExternalIdentifier).HasMaxLength(300);
        b.Property(x => x.IssueType).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Severity).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        b.HasOne(x => x.CatalogSyncRun).WithMany(x => x.Issues).HasForeignKey(x => x.CatalogSyncRunId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Species).WithMany().HasForeignKey(x => x.SpeciesId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.SpeciesExternalReference).WithMany().HasForeignKey(x => x.SpeciesExternalReferenceId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => new { x.Status, x.IssueType });
    }
}
