using System.Net;
using System.Text.Json;
using HowellSync.Commands;
using HowellSync.Execution;
using HowellSync.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using skipper_api.Data;
using skipper_api.Domain.Animals;
using Xunit;

namespace MaryAnn.Howell;

public partial class GbifImportTests
{
    private static Task<int> Sync(string connection, HowellSync.Providers.Gbif.GbifTaxon taxon, bool dry = false) =>
        Runner(connection, Exact(taxon), pageSize: 100).RunAsync(new("sync", "gbif", dry), default);

    [Fact]
    public async Task SyncRefreshesExistingIdsWithoutExpansionAndCountsUnchanged()
    {
        var (options, connection) = await Database();
        await Import(connection, Taxon(42, "Novelus originalis"));
        await using var db = new ProfessorDbContext(options);
        var before = await db.SpeciesExternalReferences.AsNoTracking().SingleAsync();
        Assert.Equal(0, await Sync(connection, Taxon(42, "Novelus revised")));
        var updated = await db.Species.AsNoTracking().SingleAsync(x => x.Id == before.SpeciesId);
        Assert.Equal("Novelus revised", updated.ScientificName);
        Assert.Equal(0, await Sync(connection, Taxon(42, "Novelus revised")));
        var current = await db.Species.AsNoTracking().SingleAsync(x => x.Id == before.SpeciesId);
        Assert.Equal(updated.UpdatedAt, current.UpdatedAt);
        var run = await db.CatalogSyncRuns.OrderByDescending(x => x.Id).FirstAsync();
        Assert.Equal(1, run.RecordsUnchanged); Assert.Equal(0, run.RecordsUpdated);
        Assert.Equal(2, run.HttpRequests); Assert.Equal(0, run.HttpRetries);
        Assert.True(run.CheckpointComplete); Assert.Equal(before.Id, run.LastCommittedReferenceId);
        Assert.Equal(new[] { before.Id }, JsonSerializer.Deserialize<int[]>(run.ReferenceIds!));
        Assert.Equal(17, await db.Species.CountAsync()); Assert.Single(await db.SpeciesAliases.ToListAsync());
        Assert.Equal(0, await Runner(connection, Exact(Taxon(99)), pageSize: 100).RunAsync(new("sync", "gbif", Taxon: 99), default));
        Assert.Single(await db.SpeciesExternalReferences.ToListAsync());
    }

    [Theory]
    [InlineData("manual")] [InlineData("edited")] [InlineData("unknown")] [InlineData("other-provider")]
    public async Task SyncProtectsManualEditedUnknownAndCompetingOwnership(string mode)
    {
        var (options, connection) = await Database();
        await Import(connection, Taxon(42, "Novelus originalis"));
        await using var db = new ProfessorDbContext(options);
        var reference = await db.SpeciesExternalReferences.Include(x => x.Species).SingleAsync();
        if (mode == "manual") reference.Species.IsManuallyManaged = true;
        if (mode == "edited") reference.Species.ScientificName = "Local correction";
        if (mode == "unknown") reference.LastAppliedScientificName = null;
        if (mode == "other-provider") db.SpeciesExternalReferences.Add(new SpeciesExternalReference {
            SpeciesId = reference.SpeciesId, Provider = "other", ExternalIdentifier = "other-id", LastAppliedScientificName = reference.Species.ScientificName });
        reference.Species.CommonName = "Curated preferred";
        reference.Species.Aliases.Add(new SpeciesAlias { Name = "Local alias", Source = "manual", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var local = reference.Species.ScientificName;
        Assert.Equal(0, await Sync(connection, Taxon(42, "External revised")));
        Assert.Equal(0, await Sync(connection, Taxon(42, "External revised")));
        db.ChangeTracker.Clear();
        var species = await db.Species.FindAsync(reference.SpeciesId);
        Assert.Equal(local, species!.ScientificName); Assert.Equal("Curated preferred", species.CommonName);
        Assert.True(await db.SpeciesAliases.AnyAsync(x => x.Source == "manual" && x.Name == "Local alias"));
        var issue = await db.CatalogSyncIssues.SingleAsync(x => x.IssueType == "ScientificNameConflict");
        Assert.Equal(reference.SpeciesId, issue.SpeciesId); Assert.Equal(reference.Id, issue.SpeciesExternalReferenceId);
        issue.Status = CatalogIssueStatus.Investigating; await db.SaveChangesAsync();
        Assert.Equal(0, await Sync(connection, Taxon(42, "External revised")));
        db.ChangeTracker.Clear();
        Assert.Equal(CatalogIssueStatus.Investigating, (await db.CatalogSyncIssues.FindAsync(issue.Id))!.Status);
    }

    [Fact]
    public async Task MissingTaxonDeduplicatesIssuesAndNeverDeletesLocalSpecies()
    {
        var (options, connection) = await Database(); await Import(connection, Taxon());
        var missing = new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        Assert.Equal(0, await Runner(connection, missing).RunAsync(new("sync", "gbif"), default));
        Assert.Equal(0, await Runner(connection, missing).RunAsync(new("sync", "gbif"), default));
        await using var db = new ProfessorDbContext(options);
        Assert.Equal(16, await db.Species.CountAsync()); Assert.Single(await db.SpeciesExternalReferences.ToListAsync());
        Assert.Single(await db.CatalogSyncIssues.ToListAsync());
        Assert.Equal("MissingExternalRecord", (await db.CatalogSyncIssues.SingleAsync()).IssueType);
    }

    [Fact]
    public async Task BudgetPartialRetryResumesExactCommittedScopeAndPreservesHistory()
    {
        var (options, connection) = await Database();
        await Import(connection, Taxon(42, "First species")); await Import(connection, Taxon(43, "Second species"));
        var requested = new List<int>();
        var handler = new Handler((r, _) => {
            var path = r.RequestUri!.AbsolutePath;
            if (path.EndsWith("vernacularNames")) return Task.FromResult(Json(new HowellSync.Providers.Gbif.GbifPage<HowellSync.Providers.Gbif.GbifVernacularName>(0, 100, true, [])));
            var key = int.Parse(path.Split('/').Last()); requested.Add(key);
            return Task.FromResult(Json(Taxon(key, key == 42 ? "First species" : "Second species")));
        });
        Assert.Equal(4, await Runner(connection, handler, budget: 2, pageSize: 100).RunAsync(new("sync", "gbif", Limit: 2), default));
        await using var db = new ProfessorDbContext(options);
        var partial = await db.CatalogSyncRuns.AsNoTracking().OrderByDescending(x => x.Id).FirstAsync();
        Assert.Equal(CatalogSyncStatus.Partial, partial.Status); Assert.Equal(1, partial.RecordsProcessed);
        Assert.False(partial.CheckpointComplete); Assert.Equal(2, partial.HttpRequests);
        Assert.NotEqual(0, partial.LastCommittedReferenceId);
        requested.Clear();
        Assert.Equal(0, await Runner(connection, handler, pageSize: 100).RunAsync(new("sync", "gbif", RetryRun: partial.Id), default));
        Assert.Equal(new[] { 43 }, requested);
        var retry = await db.CatalogSyncRuns.OrderByDescending(x => x.Id).FirstAsync();
        Assert.Equal(partial.Id, retry.RetryOfRunId); Assert.True(retry.CheckpointComplete);
        Assert.Equal(partial.ReferenceIds, retry.ReferenceIds); Assert.Equal(1, retry.RecordsProcessed);
        Assert.Equal(CatalogSyncStatus.Partial, (await db.CatalogSyncRuns.FindAsync(partial.Id))!.Status);
        Assert.Equal(2, await Runner(connection, handler).RunAsync(new("import", "gbif", RetryRun: partial.Id), default));
        Assert.Equal(2, await Runner(connection, handler).RunAsync(new("sync", "gbif", RetryRun: retry.Id), default));
    }

    [Fact]
    public async Task SyncDryRunPreservesCatalogHistoryIssuesAndCheckpoints()
    {
        var (options, connection) = await Database(); await Import(connection, Taxon(42, "Original species"));
        await using var db = new ProfessorDbContext(options);
        var before = await db.SpeciesExternalReferences.AsNoTracking().SingleAsync();
        Assert.Equal(0, await Sync(connection, Taxon(42, "Proposed species") with { TaxonomicStatus = "DOUBTFUL" }, dry: true));
        var after = await db.SpeciesExternalReferences.AsNoTracking().SingleAsync();
        Assert.Equal(before.ExternalScientificName, after.ExternalScientificName); Assert.Equal(before.LastCheckedAt, after.LastCheckedAt);
        Assert.Empty(await db.CatalogSyncIssues.ToListAsync()); Assert.Empty(await db.SpeciesAliases.ToListAsync());
        Assert.Single(await db.CatalogSyncRuns.ToListAsync());
    }

    [Fact]
    public async Task ReferenceWithoutDatasetProducesIssueWithoutHttpAndIdentityMismatchIsPreserved()
    {
        var (options, connection) = await Database();
        await using var db = new ProfessorDbContext(options);
        db.SpeciesExternalReferences.Add(new SpeciesExternalReference { SpeciesId = 4, Provider = "gbif", ExternalIdentifier = "42" });
        await db.SaveChangesAsync();
        var calls = 0;
        var handler = new Handler((_, _) => { calls++; return Task.FromResult(Json(Taxon())); });
        Assert.Equal(0, await Runner(connection, handler).RunAsync(new("sync", "gbif"), default)); Assert.Equal(0, calls);
        var reference = await db.SpeciesExternalReferences.SingleAsync(); reference.DatasetIdentifier = Dataset; await db.SaveChangesAsync();
        Assert.Equal(0, await Sync(connection, Taxon(99)));
        db.ChangeTracker.Clear();
        Assert.Equal("42", (await db.SpeciesExternalReferences.SingleAsync()).ExternalIdentifier);
        Assert.True(await db.CatalogSyncIssues.AnyAsync(x => x.IssueType == "ExternalIdentityConflict"));
    }

    [Fact]
    public async Task CheckpointAndCatalogRollbackTogetherThenReplaySafely()
    {
        var (options, connection) = await Database();
        await using var session = await CatalogSession.TryOpenAsync(connection, default);
        var id = await new CatalogRunRepository().StartAsync(session!, "gbif", CatalogSyncOperation.Synchronization, DateTime.UtcNow, default);
        var context = new CatalogExecutionContext(session!, id, "gbif", false);
        await context.BatchAsync((db, _) => { db.Species.Add(new Species { ScientificName = "Committed species", AnimalGroupId = 3 }); return Task.FromResult(new CatalogCounts(1, 1)); }, default, 10);
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.BatchAsync(async (db, token) => {
            db.Species.Add(new Species { ScientificName = "Rolled back species", AnimalGroupId = 3 });
            await db.SaveChangesAsync(token); throw new InvalidOperationException("Crash before checkpoint commit");
        }, default, 20));
        await using var observer = new ProfessorDbContext(options);
        Assert.Equal(10, (await observer.CatalogSyncRuns.FindAsync(id))!.LastCommittedReferenceId);
        Assert.True(await observer.Species.AnyAsync(x => x.ScientificName == "Committed species"));
        Assert.False(await observer.Species.AnyAsync(x => x.ScientificName == "Rolled back species"));
    }
    [Fact]
    public async Task AnimalsAndApiRemainLinkedAfterTaxonomyChanges()
    {
        var (options, connection) = await Database(); await Import(connection, Taxon());
        await using var db = new ProfessorDbContext(options);
        var enclosure = new skipper_api.Domain.Enclosures.Enclosure {
            Name = "Habitat", Type = skipper_api.Domain.Enclosures.EnclosureType.Terrarium, Location = "Room",
            Mobility = skipper_api.Domain.Enclosures.EnclosureMobility.Fixed, Status = skipper_api.Domain.Enclosures.EnclosureStatus.Active,
            CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow };
        db.Enclosures.Add(enclosure); await db.SaveChangesAsync();
        var service = new skipper_api.Services.Animals.AnimalService(db);
        var created = await service.CreateAsync(new skipper_api.Dtos.Animals.CreateAnimalRequestDto {
            EnclosureId = enclosure.Id, Name = "Mango", SpeciesId = 4, AnimalGroupId = 3,
            Status = AnimalStatus.Active, Sex = AnimalSex.Unknown, AcquiredDate = new DateOnly(2026, 1, 1) });
        Assert.Equal(skipper_api.Services.Animals.CreateAnimalResultStatus.Created, created.Status);
        var originalId = created.Animal!.Id;
        Assert.Equal(0, await Sync(connection, Taxon() with { TaxonomicStatus = "SYNONYM", AcceptedKey = 99 }));
        db.ChangeTracker.Clear();
        Assert.Equal(4, (await service.GetByIdAsync(originalId))!.SpeciesId);
        Assert.Equal(16, await db.Species.CountAsync());
        Assert.True(await db.CatalogSyncIssues.AnyAsync(x => x.IssueType == "AcceptedTaxonConflict" && x.SpeciesId == 4));
    }

    [Fact]
    public async Task SyncAddsNamesPreservesAbsentNamesAndIgnoresWhitespaceOnlyChanges()
    {
        var (options, connection) = await Database();
        await Import(connection, Taxon(), false, new HowellSync.Providers.Gbif.GbifVernacularName("Royal python", "eng", null));
        var next = Taxon() with { ScientificName = " Python   regius (Author, 1800) ", CanonicalName = " Python  regius " };
        var handler = Exact(next, new HowellSync.Providers.Gbif.GbifVernacularName("Python royal", "fra", null));
        Assert.Equal(0, await Runner(connection, handler, pageSize: 100).RunAsync(new("sync", "gbif"), default));
        Assert.Equal(0, await Runner(connection, handler, pageSize: 100).RunAsync(new("sync", "gbif"), default));
        await using var db = new ProfessorDbContext(options);
        Assert.Equal(2, await db.SpeciesCommonNames.CountAsync());
        Assert.Single(await db.CatalogSyncIssues.Where(x => x.IssueType == "CommonNamesAbsent").ToListAsync());
        Assert.Equal(Taxon().ScientificName, (await db.SpeciesExternalReferences.SingleAsync()).ExternalScientificName);
        Assert.Equal(1, (await db.CatalogSyncRuns.OrderByDescending(x => x.Id).FirstAsync()).RecordsUnchanged);
    }

    [Fact]
    public async Task RetryContendsWithActiveSyncAndRejectsChangedDataset()
    {
        var (options, connection) = await Database(); await Import(connection, Taxon());
        Assert.Equal(4, await Runner(connection, Exact(Taxon()), budget: 1, pageSize: 100).RunAsync(new("sync", "gbif"), default));
        await using var db = new ProfessorDbContext(options);
        var failed = await db.CatalogSyncRuns.OrderByDescending(x => x.Id).FirstAsync();
        await using (var session = await CatalogSession.TryOpenAsync(connection, default))
            Assert.Equal(3, await Runner(connection, Exact(Taxon())).RunAsync(new("sync", "gbif", RetryRun: failed.Id), default));
        failed.DatasetIdentifier = "11111111-1111-1111-1111-111111111111"; await db.SaveChangesAsync();
        Assert.Equal(2, await Runner(connection, Exact(Taxon())).RunAsync(new("sync", "gbif", RetryRun: failed.Id), default));
    }

    [Fact]
    public async Task ImportRetryReplaysOriginalScopeWithoutDuplicateWrites()
    {
        var (options, connection) = await Database();
        Assert.Equal(4, await Runner(connection, Exact(Taxon()), budget: 1, pageSize: 100).RunAsync(Command(), default));
        await using var db = new ProfessorDbContext(options);
        var failed = await db.CatalogSyncRuns.SingleAsync();
        Assert.Equal(0, await Runner(connection, Exact(Taxon()), pageSize: 100).RunAsync(new("import", "gbif", RetryRun: failed.Id), default));
        Assert.Single(await db.SpeciesExternalReferences.ToListAsync());
        Assert.Equal(0, await Runner(connection, Exact(Taxon()), pageSize: 100).RunAsync(new("import", "gbif", RetryRun: failed.Id), default));
        Assert.Single(await db.SpeciesExternalReferences.ToListAsync());
        Assert.Equal(1, (await db.CatalogSyncRuns.OrderByDescending(x => x.Id).FirstAsync()).RecordsUnchanged);
        db.ChangeTracker.Clear(); Assert.Equal(CatalogSyncStatus.Partial, (await db.CatalogSyncRuns.FindAsync(failed.Id))!.Status);
    }

    [Fact]
    public async Task AbandonedSyncRetryDryRunAndLostCheckpointReplayAreSafe()
    {
        var (options, connection) = await Database(); await Import(connection, Taxon());
        Assert.Equal(0, await Sync(connection, Taxon()));
        await using var db = new ProfessorDbContext(options);
        var abandoned = await db.CatalogSyncRuns.OrderByDescending(x => x.Id).FirstAsync();
        abandoned.Status = CatalogSyncStatus.Running; abandoned.CompletedAt = null;
        abandoned.CheckpointComplete = false; abandoned.LastCommittedReferenceId = 0;
        await db.SaveChangesAsync();
        var before = await db.SpeciesExternalReferences.AsNoTracking().SingleAsync();
        Assert.Equal(0, await Runner(connection, Exact(Taxon(42, "Proposed name")), pageSize: 100).RunAsync(new("sync", "gbif", DryRun: true, RetryRun: abandoned.Id), default));
        db.ChangeTracker.Clear();
        Assert.Equal(CatalogSyncStatus.Running, (await db.CatalogSyncRuns.FindAsync(abandoned.Id))!.Status);
        Assert.Equal(2, await db.CatalogSyncRuns.CountAsync());
        Assert.Equal(before.LastCheckedAt, (await db.SpeciesExternalReferences.AsNoTracking().SingleAsync()).LastCheckedAt);
        Assert.Equal(0, await Runner(connection, Exact(Taxon()), pageSize: 100).RunAsync(new("sync", "gbif", RetryRun: abandoned.Id), default));
        db.ChangeTracker.Clear();
        Assert.Equal(CatalogSyncStatus.Failed, (await db.CatalogSyncRuns.FindAsync(abandoned.Id))!.Status);
        Assert.Single(await db.SpeciesExternalReferences.ToListAsync());
        var replay = await db.CatalogSyncRuns.OrderByDescending(x => x.Id).FirstAsync();
        Assert.Equal(1, replay.RecordsUnchanged); Assert.Equal(abandoned.Id, replay.RetryOfRunId);
    }

    [Fact]
    public async Task DatabaseRejectsDuplicateOpenIssueButRetainsResolvedHistory()
    {
        var (options, connection) = await Database(); await Import(connection, Taxon());
        await Sync(connection, Taxon(42, "Changed curated name"));
        await using var db = new ProfessorDbContext(options);
        var original = await db.CatalogSyncIssues.SingleAsync(x => x.IssueType == "ScientificNameConflict");
        db.CatalogSyncIssues.Add(new CatalogSyncIssue { Provider = "gbif", IssueType = original.IssueType,
            Fingerprint = original.Fingerprint, Description = "Duplicate condition" });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        original = await db.CatalogSyncIssues.SingleAsync(x => x.IssueType == "ScientificNameConflict");
        original.Status = CatalogIssueStatus.Resolved; original.ResolvedAt = DateTime.UtcNow; await db.SaveChangesAsync();
        await Sync(connection, Taxon(42, "Changed curated name"));
        db.ChangeTracker.Clear();
        Assert.Equal(2, await db.CatalogSyncIssues.CountAsync(x => x.IssueType == "ScientificNameConflict"));
        Assert.Equal(CatalogIssueStatus.Resolved, (await db.CatalogSyncIssues.FindAsync(original.Id))!.Status);
    }

    [Fact]
    public async Task SynchronizationMigrationPreservesLegacyOwnershipAndRollsBackCleanly()
    {
        var (options, _) = await Database();
        await using var db = new ProfessorDbContext(options);
        var migrator = db.GetService<IMigrator>();
        const string previous = "20261017210000_MigrateLegacyAnimalReferences";
        await migrator.MigrateAsync(previous);
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO "SpeciesExternalReferences" ("SpeciesId", "Provider", "DatasetIdentifier", "ExternalIdentifier")
            VALUES (4, 'gbif', 'd7dddbf4-2cf0-4f39-9b2a-bb099caae36c', '42');
            """);
        await migrator.MigrateAsync();
        var reference = await db.SpeciesExternalReferences.AsNoTracking().SingleAsync();
        Assert.Equal(4, reference.SpeciesId); Assert.Null(reference.LastAppliedScientificName);
        Assert.False(db.Database.HasPendingModelChanges());
        await migrator.MigrateAsync(previous);
        Assert.Equal(1, await db.SpeciesExternalReferences.CountAsync());
        await migrator.MigrateAsync();
        Assert.Equal(reference.Id, (await db.SpeciesExternalReferences.SingleAsync()).Id);
    }

}
