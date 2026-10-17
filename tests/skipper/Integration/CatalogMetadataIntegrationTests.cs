using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using skipper_api.Data;
using skipper_api.Domain.Animals;
using Xunit;

namespace Skipper.Api.Tests.Data;

public class CatalogMetadataIntegrationTests(PostgresFixture database) : IClassFixture<PostgresFixture>
{
    private const string PreviousMigration = "20261017170959_AddAnimalCatalogSearchIndexes";

    private static SpeciesExternalReference Reference(int species = 4, string provider = "gbif", string? dataset = null, string identifier = "taxon:abc") =>
        new() { SpeciesId = species, Provider = provider, DatasetIdentifier = dataset, ExternalIdentifier = identifier };

    private static async Task Reject(ProfessorDbContext db, object entity, string state)
    {
        db.Add(entity);
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(state, Assert.IsType<PostgresException>(error.InnerException).SqlState);
        db.ChangeTracker.Clear();
    }

    [Fact]
    public async Task ExternalReferences_PersistProviderMetadataAndEnforceScopedIdentity()
    {
        await using var db = new ProfessorDbContext(await database.CreateDatabaseAsync());
        var reference = Reference();
        reference.ExternalScientificName = "Provider spelling";
        reference.ExternalTaxonomicStatus = "UNKNOWN_PROVIDER_STATUS";
        reference.ExternalAcceptedIdentifier = "not-yet-imported";
        db.AddRange(reference, Reference(provider: "col"), Reference(dataset: "checklist-a"), Reference(dataset: "checklist-b"));
        await db.SaveChangesAsync();
        var timestamp = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);
        reference.LastCheckedAt = timestamp;
        reference.UpdatedAt = timestamp.AddDays(-1);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var saved = await db.SpeciesExternalReferences.SingleAsync(x => x.Id == reference.Id);
        Assert.Equal(4, saved.SpeciesId);
        Assert.Equal("UNKNOWN_PROVIDER_STATUS", saved.ExternalTaxonomicStatus);
        Assert.Equal("Provider spelling", saved.ExternalScientificName);
        Assert.Equal("not-yet-imported", saved.ExternalAcceptedIdentifier);
        Assert.Equal(timestamp, saved.LastCheckedAt);
        Assert.Equal(timestamp.AddDays(-1), saved.UpdatedAt);
        Assert.True(saved.CreatedAt.Year >= 2026);
        Assert.Equal(4, await db.SpeciesExternalReferences.CountAsync());
        await Reject(db, Reference(species: 5), PostgresErrorCodes.UniqueViolation);
        await Reject(db, Reference(species: 5, dataset: "checklist-a"), PostgresErrorCodes.UniqueViolation);
        await Reject(db, Reference(species: 999999, identifier: "missing"), PostgresErrorCodes.ForeignKeyViolation);
        await Reject(db, Reference(provider: "GBIF", identifier: "other"), PostgresErrorCodes.CheckViolation);
        await Reject(db, Reference(dataset: "", identifier: "other"), PostgresErrorCodes.CheckViolation);
        db.Add(Reference(species: 5, identifier: "different"));
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CommonNames_AllowLanguagesSourcesAndSharedNames_ButRejectNormalizedDuplicates()
    {
        await using var db = new ProfessorDbContext(await database.CreateDatabaseAsync());
        Assert.Empty(await db.SpeciesCommonNames.ToListAsync());
        db.Add(new SpeciesCommonName { SpeciesId = 4, Name = "Royal python" });
        await db.SaveChangesAsync();
        Assert.Single(await db.SpeciesCommonNames.ToListAsync());
        db.AddRange(
            new SpeciesCommonName { SpeciesId = 4, Name = "Royal python", LanguageCode = "en" },
            new SpeciesCommonName { SpeciesId = 4, Name = "Royal python", LanguageCode = "fr" },
            new SpeciesCommonName { SpeciesId = 4, Name = "Royal python", Source = "Museum" },
            new SpeciesCommonName { SpeciesId = 5, Name = "Royal python" });
        await db.SaveChangesAsync();
        await Reject(db, new SpeciesCommonName { SpeciesId = 4, Name = " ROYAL PYTHON " }, PostgresErrorCodes.UniqueViolation);
        await Reject(db, new SpeciesCommonName { SpeciesId = 4, Name = "Royal python", LanguageCode = "EN" }, PostgresErrorCodes.UniqueViolation);
        await Reject(db, new SpeciesCommonName { SpeciesId = 4, Name = "Royal python", Source = "museum" }, PostgresErrorCodes.UniqueViolation);
        var species = await db.Species.Include(x => x.CommonNames).SingleAsync(x => x.Id == 4);
        Assert.Equal(4, species.CommonNames.Count);
        Assert.Equal("Ball python", species.CommonName);
        Assert.Equal(16, await db.Species.CountAsync());
    }

    [Fact]
    public async Task AliasesAndCuratedSpecies_RetainIdentityAndTaxonomicMetadata()
    {
        await using var db = new ProfessorDbContext(await database.CreateDatabaseAsync());
        foreach (var type in Enum.GetValues<SpeciesAliasType>())
            db.Add(new SpeciesAlias { SpeciesId = 4, Name = "Historical python", AliasType = type });
        await db.SaveChangesAsync();
        await Reject(db, new SpeciesAlias { SpeciesId = 4, Name = "HISTORICAL PYTHON", AliasType = SpeciesAliasType.Synonym }, PostgresErrorCodes.UniqueViolation);
        var species = await db.Species.SingleAsync(x => x.Id == 4);
        Assert.True(species.IsManuallyManaged);
        Assert.Equal(TaxonomicStatus.Unresolved, species.TaxonomicStatus);
        Assert.Null(species.CreatedAt);
        species.ScientificName = "Curated scientific name";
        species.TaxonomicStatus = TaxonomicStatus.Accepted;
        species.LastSyncedAt = DateTime.UtcNow;
        species.UpdatedAt = species.LastSyncedAt;
        db.Add(new Species { ScientificName = "Imported species", AnimalGroupId = 7, IsManuallyManaged = false, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        species = await db.Species.Include(x => x.Aliases).SingleAsync(x => x.Id == 4);
        Assert.Equal("Curated scientific name", species.ScientificName);
        Assert.Equal(TaxonomicStatus.Accepted, species.TaxonomicStatus);
        Assert.NotNull(species.LastSyncedAt);
        Assert.Equal(4, species.Aliases.Count);
        Assert.False((await db.Species.SingleAsync(x => x.ScientificName == "Imported species")).IsManuallyManaged);
    }

    [Fact]
    public async Task SyncRuns_PersistLifecycleCountsFailureAndCancellation()
    {
        await using var db = new ProfessorDbContext(await database.CreateDatabaseAsync());
        var run = new CatalogSyncRun { Provider = "gbif", DatasetIdentifier = "dataset-a", OperationType = CatalogSyncOperation.Synchronization };
        db.Add(run); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        run = await db.CatalogSyncRuns.SingleAsync();
        Assert.Equal(CatalogSyncStatus.Pending, run.Status);
        Assert.Null(run.CompletedAt); Assert.Null(run.StartedAt);
        run.Status = CatalogSyncStatus.Running; run.StartedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        foreach (var status in new[] { CatalogSyncStatus.Completed, CatalogSyncStatus.CompletedWithIssues, CatalogSyncStatus.Failed, CatalogSyncStatus.Cancelled })
        {
            run.Status = status; run.CompletedAt = DateTime.UtcNow;
            run.RecordsProcessed = 10; run.RecordsCreated = 2; run.RecordsUpdated = 3; run.RecordsSkipped = 4; run.RecordsFailed = 1;
            run.ErrorMessage = status == CatalogSyncStatus.Failed ? "Bounded failure details" : null;
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            run = await db.CatalogSyncRuns.SingleAsync();
            Assert.Equal(status, run.Status); Assert.Equal("gbif", run.Provider); Assert.Equal("dataset-a", run.DatasetIdentifier);
            Assert.Equal(10, run.RecordsProcessed); Assert.Equal(2, run.RecordsCreated); Assert.Equal(3, run.RecordsUpdated);
            Assert.Equal(4, run.RecordsSkipped); Assert.Equal(1, run.RecordsFailed); Assert.NotNull(run.CompletedAt);
            Assert.Equal(status == CatalogSyncStatus.Failed ? "Bounded failure details" : null, run.ErrorMessage);
        }
        await Reject(db, new CatalogSyncRun { Provider = "col", RecordsProcessed = -1 }, PostgresErrorCodes.CheckViolation);
    }

    [Fact]
    public async Task Issues_PreserveHistoryWhenMetadataIsDeleted_AndRestrictRunDeletion()
    {
        await using var db = new ProfessorDbContext(await database.CreateDatabaseAsync());
        var species = new Species { ScientificName = "Temporary species", AnimalGroupId = 7 };
        db.Add(species); await db.SaveChangesAsync();
        var reference = Reference(species.Id);
        var run = new CatalogSyncRun { Provider = "gbif" };
        db.AddRange(reference, run); await db.SaveChangesAsync();
        var issue = new CatalogSyncIssue { Provider = "gbif", ExternalIdentifier = reference.ExternalIdentifier, CatalogSyncRunId = run.Id, SpeciesId = species.Id, SpeciesExternalReferenceId = reference.Id, IssueType = "ConflictingScientificNames", Description = "Needs review" };
        db.AddRange(issue, new CatalogSyncIssue { Provider = "col", IssueType = "MissingAcceptedTaxon", Description = "No internal match", ExternalIdentifier = "unmatched" });
        await db.SaveChangesAsync();
        foreach (var status in new[] { CatalogIssueStatus.Investigating, CatalogIssueStatus.Ignored, CatalogIssueStatus.Resolved })
        {
            issue.Status = status; issue.ResolvedAt = status == CatalogIssueStatus.Resolved ? DateTime.UtcNow : null;
            await db.SaveChangesAsync();
        }
        db.ChangeTracker.Clear();
        await db.SpeciesExternalReferences.Where(x => x.Id == reference.Id).ExecuteDeleteAsync();
        await db.Species.Where(x => x.Id == species.Id).ExecuteDeleteAsync();
        issue = await db.CatalogSyncIssues.SingleAsync(x => x.Id == issue.Id);
        Assert.Null(issue.SpeciesId); Assert.Null(issue.SpeciesExternalReferenceId);
        Assert.Equal(run.Id, issue.CatalogSyncRunId); Assert.Equal("taxon:abc", issue.ExternalIdentifier);
        Assert.Equal(CatalogIssueStatus.Resolved, issue.Status); Assert.NotNull(issue.ResolvedAt);
        var error = await Assert.ThrowsAsync<PostgresException>(() => db.CatalogSyncRuns.Where(x => x.Id == run.Id).ExecuteDeleteAsync());
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, error.SqlState);
        Assert.Equal(2, await db.CatalogSyncIssues.CountAsync());
    }

    [Fact]
    public async Task MigrationAndRollback_PreserveExistingCatalogAnimalsAndSeeds()
    {
        await using var db = new ProfessorDbContext(await database.CreateDatabaseAsync());
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration);
        await db.Database.ExecuteSqlRawAsync("""
            UPDATE "Species" SET "ScientificName" = 'Curated Python', "CommonName" = 'Curated display' WHERE "Id" = 4;
            INSERT INTO "Enclosures" ("Name", "Type", "Location", "Mobility", "Status", "CreatedDate", "UpdatedDate")
            VALUES ('Habitat', 'Terrarium', 'Room', 'Fixed', 'Active', NOW(), NOW());
            INSERT INTO "Animals" ("EnclosureId", "Name", "Species", "SubspeciesOrMorph", "AnimalType", "Status", "Sex", "AcquiredDate", "BirthDateIsEstimated", "CreatedAt", "UpdatedAt", "AnimalGroupId", "SpeciesId", "AnimalVariantId")
            VALUES (1, 'Structured', 'Historical name', 'Historical morph', 'Reptile', 'Active', 'Unknown', DATE '2026-01-01', false, NOW(), NOW(), 3, 4, 3),
                   (1, 'Custom', 'Custom species', 'Custom variant', 'Herptile', 'Active', 'Unknown', DATE '2026-01-01', false, NOW(), NOW(), NULL, NULL, NULL),
                   (1, 'Custom morph', 'Historical name', 'Custom morph', 'Reptile', 'Active', 'Unknown', DATE '2026-01-01', false, NOW(), NOW(), 3, 4, NULL);
            """);
        var before = await Snapshot(db);
        await migrator.MigrateAsync(); await migrator.MigrateAsync();
        Assert.Equal(before, await Snapshot(db));
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal(7, await db.AnimalGroups.CountAsync()); Assert.Equal(16, await db.Species.CountAsync()); Assert.Equal(4, await db.AnimalVariants.CountAsync());
        Assert.Equal("Curated Python", (await db.Species.SingleAsync(x => x.Id == 4)).ScientificName);
        foreach (var table in new[] { "SpeciesCommonNames", "SpeciesAliases", "SpeciesExternalReferences", "CatalogSyncRuns", "CatalogSyncIssues" })
            Assert.True(await db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM pg_tables WHERE schemaname = 'public' AND tablename = {table}").SingleAsync() == 1);
        var indexes = await db.Database.SqlQueryRaw<string>("SELECT indexdef AS \"Value\" FROM pg_indexes WHERE tablename = 'SpeciesExternalReferences'").ToListAsync();
        Assert.Contains(indexes, x => x.Contains("UNIQUE") && x.Contains("NULLS NOT DISTINCT"));
        db.Add(Reference()); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var error = await Assert.ThrowsAsync<PostgresException>(() => db.Species.Where(x => x.Id == 4).ExecuteDeleteAsync());
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, error.SqlState);
        await migrator.MigrateAsync(PreviousMigration);
        Assert.Equal(before, await Snapshot(db));
        await migrator.MigrateAsync();
        Assert.Equal(before, await Snapshot(db)); Assert.Empty(await db.SpeciesExternalReferences.ToListAsync());
    }

    private static async Task<string[]> Snapshot(ProfessorDbContext db) => (await db.Database.SqlQueryRaw<string>("""
        SELECT row_to_json(a)::text AS "Value" FROM "Animals" a
        UNION ALL SELECT row_to_json(g)::text FROM "AnimalGroups" g
        UNION ALL SELECT row_to_json(v)::text FROM "AnimalVariants" v
        UNION ALL SELECT row_to_json(s)::text FROM (SELECT "Id", "CommonName", "ScientificName", "AnimalGroupId", "IsActive", "PreferredVariantType" FROM "Species") s
        """).ToListAsync()).Order().ToArray();
}
