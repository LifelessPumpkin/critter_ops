using System.Net;
using System.Text.Json;
using HowellSync.Commands;
using HowellSync.Execution;
using HowellSync.Persistence;
using HowellSync.Providers;
using HowellSync.Providers.Gbif;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using skipper_api.Data;
using skipper_api.Domain.Animals;
using Skipper.Api.Tests.Data;
using Xunit;

namespace MaryAnn.Howell;

public class GbifImportTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private const string Dataset = "d7dddbf4-2cf0-4f39-9b2a-bb099caae36c";
    private static GbifTaxon Taxon(int key = 42, string name = "Python regius") => new() {
        Key = key, DatasetKey = Dataset, ScientificName = name + " (Author, 1800)", CanonicalName = name,
        Rank = "SPECIES", TaxonomicStatus = "ACCEPTED", Kingdom = "Animalia", Phylum = "Chordata", Class = "Reptilia" };
    private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web))) };
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => response(request, token);
    }
    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan };
    }
    private static CatalogOperationRunner Runner(string connection, HttpMessageHandler handler, int budget = 100, int pageSize = 1)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["ConnectionStrings:ProfessorDb"] = connection, ["Providers:gbif:UserAgent"] = "CritterOps/1.0 (+https://critterops.test/contact)",
            ["Providers:gbif:MinimumRequestInterval"] = "00:00:00", ["Providers:gbif:MaximumRetries"] = "0",
            ["Providers:gbif:MaximumRequestsPerRun"] = budget.ToString(), ["Providers:gbif:PageSize"] = pageSize.ToString()
        }).Build();
        return new(new ProviderRegistry([new GbifCatalogProvider(new Factory(handler), NullLogger<GbifCatalogProvider>.Instance)]), config,
            NullLogger<CatalogOperationRunner>.Instance, new CatalogSessionFactory(), new CatalogRunRepository());
    }
    private static Handler Exact(GbifTaxon taxon, params GbifVernacularName[] names) => new((request, _) => Task.FromResult(
        request.RequestUri!.AbsolutePath.EndsWith("vernacularNames") ? Json(new GbifPage<GbifVernacularName>(0, 100, true, names.ToList())) : Json(taxon)));
    private async Task<(DbContextOptions<ProfessorDbContext> Options, string Connection)> Database()
    {
        var options = await fixture.CreateDatabaseAsync();
        using var db = new ProfessorDbContext(options);
        return (options, db.Database.GetConnectionString()!);
    }
    private static CatalogCommand Command(int key = 42, bool dry = false) => new("import", "gbif", dry, Taxon: key);
    private static Task<int> Import(string connection, GbifTaxon taxon, bool dry = false, params GbifVernacularName[] names) =>
        Runner(connection, Exact(taxon, names), pageSize: 100).RunAsync(Command(taxon.Key!.Value, dry), default);

    [Fact]
    public async Task SeedMatchNormalizesNamesPreservesIdentityAndImportsNamesIdempotently()
    {
        var (options, connection) = await Database();
        var taxon = Taxon() with { CanonicalName = "  PYTHON\t regius " };
        GbifVernacularName[] names = [new(" Royal  python ", "eng", "Checklist citation"), new("Royal python", "ENG", "Checklist citation"), new("Python royal", "fra", null)];
        Assert.Equal(0, await Import(connection, taxon, false, names));
        Assert.Equal(0, await Import(connection, taxon, false, names));
        await using var db = new ProfessorDbContext(options);
        Assert.Equal(16, await db.Species.CountAsync());
        var reference = await db.SpeciesExternalReferences.SingleAsync();
        Assert.Equal(4, reference.SpeciesId); Assert.Equal(Dataset, reference.DatasetIdentifier);
        Assert.Equal(taxon.ScientificName, reference.ExternalScientificName); Assert.Equal("42", reference.ExternalIdentifier);
        var seed = await db.Species.FindAsync(4); Assert.Equal("Python regius", seed!.ScientificName); Assert.Equal("Ball python", seed.CommonName);
        Assert.True(seed.IsManuallyManaged); Assert.Equal(2, await db.SpeciesCommonNames.CountAsync());
        Assert.Contains(await db.SpeciesCommonNames.ToListAsync(), n => n.LanguageCode == "eng" && n.Source!.Contains("Checklist citation"));
        Assert.All(await db.CatalogSyncRuns.ToListAsync(), r => { Assert.Equal(CatalogSyncStatus.Completed, r.Status); Assert.Equal(Dataset, r.DatasetIdentifier); Assert.Equal(1, r.RecordsProcessed); });
    }

    [Fact]
    public async Task NewSpeciesRenamePreservesIdAndHistoricalAlias()
    {
        var (options, connection) = await Database();
        Assert.Equal(0, await Import(connection, Taxon(42, "Novelus originalis")));
        await using var db = new ProfessorDbContext(options);
        var id = (await db.SpeciesExternalReferences.SingleAsync()).SpeciesId;
        Assert.Equal(0, await Import(connection, Taxon(42, "Novelus revised")));
        Assert.Equal(0, await Import(connection, Taxon(42, "Novelus revised")));
        db.ChangeTracker.Clear();
        Assert.Equal(id, (await db.SpeciesExternalReferences.SingleAsync()).SpeciesId);
        var species = await db.Species.FindAsync(id); Assert.Equal("Novelus revised", species!.ScientificName); Assert.False(species.IsManuallyManaged);
        Assert.Equal("Novelus originalis", (await db.SpeciesAliases.SingleAsync()).Name);
    }

    [Fact]
    public async Task CuratedNameAndClassificationAreNeverOverwritten()
    {
        var (options, connection) = await Database();
        Assert.Equal(0, await Import(connection, Taxon()));
        Assert.Equal(0, await Import(connection, Taxon(42, "Changed name")));
        Assert.Equal(0, await Import(connection, Taxon() with { Class = "Mammalia" }));
        await using var db = new ProfessorDbContext(options);
        var species = await db.Species.FindAsync(4); Assert.Equal("Python regius", species!.ScientificName); Assert.Equal(3, species.AnimalGroupId);
        Assert.Contains(await db.CatalogSyncIssues.ToListAsync(), x => x.IssueType == "ScientificNameConflict");
        Assert.Contains(await db.CatalogSyncIssues.ToListAsync(), x => x.IssueType == "ClassificationConflict");
    }

    [Fact]
    public async Task SynonymBeforeAcceptedDefersThenRestartResolvesWithoutDuplicateSpecies()
    {
        var (options, connection) = await Database();
        var synonym = Taxon(43, "Python historical") with { TaxonomicStatus = "SYNONYM", AcceptedKey = 42 };
        Assert.Equal(0, await Import(connection, synonym));
        await using var db = new ProfessorDbContext(options);
        Assert.Empty(await db.SpeciesExternalReferences.ToListAsync());
        var issue = await db.CatalogSyncIssues.SingleAsync(); Assert.Contains("42", issue.Description); Assert.Equal("43", issue.ExternalIdentifier);
        Assert.Equal(0, await Import(connection, Taxon()));
        Assert.Equal(0, await Import(connection, synonym)); Assert.Equal(0, await Import(connection, synonym));
        Assert.Equal(16, await db.Species.CountAsync()); Assert.Equal(2, await db.SpeciesExternalReferences.CountAsync());
        var alias = await db.SpeciesAliases.SingleAsync(); Assert.Equal(4, alias.SpeciesId); Assert.Equal(SpeciesAliasType.Synonym, alias.AliasType);
        var reference = await db.SpeciesExternalReferences.SingleAsync(x => x.ExternalIdentifier == "43");
        Assert.Equal("42", reference.ExternalAcceptedIdentifier); Assert.Equal(4, reference.SpeciesId);
    }

    [Theory]
    [InlineData("SYNONYM")] [InlineData("DOUBTFUL")] [InlineData("UNKNOWN")]
    public async Task StatusChangesRefreshMetadataButNeverReassignOrDelete(string status)
    {
        var (options, connection) = await Database();
        await Import(connection, Taxon());
        var changed = Taxon() with { TaxonomicStatus = status, AcceptedKey = 99 };
        await Import(connection, changed); await Import(connection, changed);
        await using var db = new ProfessorDbContext(options);
        var reference = await db.SpeciesExternalReferences.SingleAsync();
        Assert.Equal(status, reference.ExternalTaxonomicStatus); Assert.Equal("99", reference.ExternalAcceptedIdentifier); Assert.Equal(4, reference.SpeciesId);
        Assert.Equal(16, await db.Species.CountAsync()); Assert.Equal(2, await db.CatalogSyncIssues.CountAsync(x => x.IssueType == "TaxonomyChanged"));
    }

    [Fact]
    public async Task SynonymPromotionStaysBlockedUntilReviewed()
    {
        var (options, connection) = await Database();
        await Import(connection, Taxon());
        await Import(connection, Taxon(43, "Alias name") with { TaxonomicStatus = "SYNONYM", AcceptedKey = 42 });
        await Import(connection, Taxon(43, "New accepted")); await Import(connection, Taxon(43, "New accepted"));
        await Import(connection, Taxon(44, "Dependent synonym") with { TaxonomicStatus = "SYNONYM", AcceptedKey = 43 });
        await using var db = new ProfessorDbContext(options);
        Assert.Equal(16, await db.Species.CountAsync()); Assert.Equal("Python regius", (await db.Species.FindAsync(4))!.ScientificName);
        Assert.Equal(2, await db.SpeciesExternalReferences.CountAsync());
        Assert.All(await db.SpeciesExternalReferences.ToListAsync(), x => Assert.Equal(4, x.SpeciesId));
    }

    [Fact]
    public async Task AmbiguousOrIncompatibleNamesDoNotCreateReferences()
    {
        var (options, connection) = await Database();
        await using var db = new ProfessorDbContext(options);
        db.Species.Add(new Species { ScientificName = "\tpython   REGIUS\n", AnimalGroupId = 3 }); await db.SaveChangesAsync();
        await Import(connection, Taxon());
        Assert.Empty(await db.SpeciesExternalReferences.ToListAsync());
        Assert.Equal("AmbiguousSpeciesMatch", (await db.CatalogSyncIssues.SingleAsync()).IssueType);
    }

    [Theory]
    [InlineData("rank")] [InlineData("group")] [InlineData("name")] [InlineData("dataset")] [InlineData("status")]
    public async Task InvalidTaxaProduceIssuesWithoutCanonicalRecords(string invalid)
    {
        var (options, connection) = await Database();
        var taxon = invalid switch {
            "rank" => Taxon() with { Rank = "SUBSPECIES" }, "group" => Taxon() with { Class = "Unknown" },
            "name" => Taxon() with { ScientificName = null }, "dataset" => Taxon() with { DatasetKey = null },
            _ => Taxon() with { TaxonomicStatus = "MISAPPLIED" }
        };
        Assert.Equal(0, await Import(connection, taxon));
        await using var db = new ProfessorDbContext(options);
        Assert.Empty(await db.SpeciesExternalReferences.ToListAsync()); Assert.Equal(16, await db.Species.CountAsync());
        Assert.Single(await db.CatalogSyncIssues.ToListAsync());
        var run = await db.CatalogSyncRuns.SingleAsync(); Assert.Equal(1, run.RecordsSkipped); Assert.Equal(CatalogSyncStatus.CompletedWithIssues, run.Status);
    }

    [Fact]
    public async Task DryRunReportsAdditionsAndConflictsWithoutAnyWrites()
    {
        var (options, connection) = await Database();
        Assert.Equal(0, await Import(connection, Taxon(42, "Novelus dryrun"), true, new GbifVernacularName("Test name", "eng", null)));
        Assert.Equal(0, await Import(connection, Taxon() with { Class = "Unknown" }, true));
        await using var db = new ProfessorDbContext(options);
        Assert.Equal(16, await db.Species.CountAsync()); Assert.Empty(await db.CatalogSyncRuns.ToListAsync());
        Assert.Empty(await db.CatalogSyncIssues.ToListAsync()); Assert.Empty(await db.SpeciesCommonNames.ToListAsync());
        Assert.Empty(await db.SpeciesAliases.ToListAsync()); Assert.Empty(await db.SpeciesExternalReferences.ToListAsync());
    }

    [Theory]
    [InlineData(1, 1)] [InlineData(2, 2)] [InlineData(3, 2)]
    public async Task SearchPagesStopAtLimitOrEnd(int limit, int expected)
    {
        var (options, connection) = await Database(); var searches = 0;
        var handler = new Handler((request, _) => {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("search")) {
                Assert.Contains("datasetKey=" + Dataset, request.RequestUri.Query);
                var index = searches++; Assert.Contains("offset=" + index, request.RequestUri.Query);
                return Task.FromResult(Json(new GbifPage<GbifTaxon>(index, 1, index == 1, [Taxon(42 + index, "Novelus " + index)])));
            }
            if (path.EndsWith("vernacularNames")) return Task.FromResult(Json(new GbifPage<GbifVernacularName>(0, 1, true, [])));
            var key = int.Parse(path.Split('/').Last()); return Task.FromResult(Json(Taxon(key, "Novelus " + (key - 42))));
        });
        Assert.Equal(0, await Runner(connection, handler).RunAsync(new("import", "gbif", Query: "Novelus", Limit: limit), default));
        await using var db = new ProfessorDbContext(options);
        Assert.Equal(expected, searches); Assert.Equal(expected, await db.SpeciesExternalReferences.CountAsync());
        Assert.Equal(expected, (await db.CatalogSyncRuns.SingleAsync()).RecordsProcessed);
    }

    [Theory]
    [InlineData("budget")] [InlineData("http")] [InlineData("repeat")]
    public async Task LaterPageFailurePreservesCommittedBatchAndRestartIsIdempotent(string failure)
    {
        var (options, connection) = await Database(); var searches = 0;
        Handler CreateHandler() => new((request, _) => {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("search")) {
                var index = searches++;
                if (index == 1 && failure == "http") return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
                return Task.FromResult(Json(new GbifPage<GbifTaxon>(index, 1, false, [Taxon()])));
            }
            return Task.FromResult(path.EndsWith("vernacularNames") ? Json(new GbifPage<GbifVernacularName>(0, 1, true, [])) : Json(Taxon()));
        });
        Assert.Equal(1, await Runner(connection, CreateHandler(), failure == "budget" ? 3 : 100).RunAsync(new("import", "gbif", Query: "Python", Limit: 5), default));
        await using var db = new ProfessorDbContext(options);
        var run = await db.CatalogSyncRuns.SingleAsync(); Assert.Equal(CatalogSyncStatus.Failed, run.Status); Assert.Equal(1, run.RecordsProcessed);
        Assert.Single(await db.SpeciesExternalReferences.ToListAsync()); Assert.Single(await db.CatalogSyncIssues.ToListAsync());
        Assert.Equal(0, await Import(connection, Taxon())); Assert.Single(await db.SpeciesExternalReferences.ToListAsync());
        Assert.Equal(16, await db.Species.CountAsync());
    }

    [Fact]
    public async Task EmptySearchIsSuccessfulWithoutExtraRequests()
    {
        var (options, connection) = await Database(); var calls = 0;
        var handler = new Handler((_, _) => { calls++; return Task.FromResult(Json(new GbifPage<GbifTaxon>(0, 1, true, []))); });
        Assert.Equal(0, await Runner(connection, handler).RunAsync(new("import", "gbif", Query: "none"), default));
        Assert.Equal(1, calls);
        await using var db = new ProfessorDbContext(options); Assert.Equal(0, (await db.CatalogSyncRuns.SingleAsync()).RecordsProcessed);
    }

    [Fact]
    public async Task CancellationBetweenPagesPreservesProgressAndReleasesLock()
    {
        var (options, connection) = await Database(); using var cancel = new CancellationTokenSource(); var searches = 0;
        var handler = new Handler((request, _) => {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("search")) {
                if (searches++ == 1) { cancel.Cancel(); cancel.Token.ThrowIfCancellationRequested(); }
                return Task.FromResult(Json(new GbifPage<GbifTaxon>(0, 1, false, [Taxon()])));
            }
            return Task.FromResult(path.EndsWith("vernacularNames") ? Json(new GbifPage<GbifVernacularName>(0, 1, true, [])) : Json(Taxon()));
        });
        Assert.Equal(130, await Runner(connection, handler).RunAsync(new("import", "gbif", Query: "Python"), cancel.Token));
        await using var db = new ProfessorDbContext(options);
        var run = await db.CatalogSyncRuns.SingleAsync(); Assert.Equal(CatalogSyncStatus.Cancelled, run.Status); Assert.Equal(1, run.RecordsProcessed);
        Assert.Equal(0, await Import(connection, Taxon()));
    }

    [Fact]
    public async Task BudgetDuringNamesLeavesNoPartiallyLinkedTaxon()
    {
        var (options, connection) = await Database();
        var runner = Runner(connection, Exact(Taxon()), budget: 1, pageSize: 100);
        Assert.Equal(1, await runner.RunAsync(Command(), default));
        await using var db = new ProfessorDbContext(options);
        Assert.Empty(await db.SpeciesExternalReferences.ToListAsync()); Assert.Empty(await db.SpeciesCommonNames.ToListAsync());
        var run = await db.CatalogSyncRuns.SingleAsync(); Assert.Equal(0, run.RecordsProcessed); Assert.Equal(CatalogSyncStatus.Failed, run.Status);
        Assert.Equal("RequestBudgetExhausted", (await db.CatalogSyncIssues.SingleAsync()).IssueType);
    }

    [Fact]
    public async Task OtherProviderReferencesRemainUntouched()
    {
        var (options, connection) = await Database();
        await using var db = new ProfessorDbContext(options);
        db.SpeciesExternalReferences.Add(new SpeciesExternalReference { SpeciesId = 4, Provider = "other", ExternalIdentifier = "42", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        await Import(connection, Taxon()); await Import(connection, Taxon());
        var references = await db.SpeciesExternalReferences.ToListAsync(); Assert.Equal(2, references.Count);
        Assert.All(references, r => Assert.Equal(4, r.SpeciesId)); Assert.Null(references.Single(x => x.Provider == "other").DatasetIdentifier);
    }

    [Fact]
    public async Task DryRunRetainsProposalsAcrossBatchesWithoutDuplicateCreates()
    {
        var (options, connection) = await Database();
        await using var session = await CatalogSession.TryOpenAsync(connection, default);
        var context = new CatalogExecutionContext(session!, null, "gbif", true);
        var reconciler = new GbifSpeciesReconciler(NullLogger.Instance);
        foreach (var taxon in new[] { Taxon(42, "Proposed species"), Taxon(43, "Proposed species") })
            await context.BatchAsync((db, ct) => reconciler.ApplyAsync(db, context, taxon, [], ct), default);
        Assert.Equal(1, context.Counts.Created); Assert.Equal(1, context.Counts.Updated);
        await using var db = new ProfessorDbContext(options); Assert.Equal(16, await db.Species.CountAsync());
        Assert.Empty(await db.SpeciesExternalReferences.ToListAsync());
    }

    [Theory]
    [InlineData("key")] [InlineData("dataset")]
    public async Task IncorrectLookupIdentityFailsWithoutPersistence(string mismatch)
    {
        var (options, connection) = await Database();
        var taxon = mismatch == "key" ? Taxon(99) : Taxon() with { DatasetKey = "11111111-1111-1111-1111-111111111111" };
        Assert.Equal(1, await Runner(connection, Exact(taxon), pageSize: 100).RunAsync(Command(), default));
        await using var db = new ProfessorDbContext(options); Assert.Empty(await db.SpeciesExternalReferences.ToListAsync());
        Assert.Equal(CatalogSyncStatus.Failed, (await db.CatalogSyncRuns.SingleAsync()).Status);
    }

    [Fact]
    public async Task ExternalReferenceUniqueConstraintRollsBackSpeciesInSameBatch()
    {
        var (options, connection) = await Database(); await Import(connection, Taxon());
        await using var session = await CatalogSession.TryOpenAsync(connection, default);
        var history = new CatalogRunRepository(); var id = await history.StartAsync(session!, "gbif", CatalogSyncOperation.Import, DateTime.UtcNow, default);
        var context = new CatalogExecutionContext(session!, id, "gbif", false);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.BatchAsync((db, _) => {
            var species = new Species { ScientificName = "Rollback linked species", AnimalGroupId = 3 };
            species.ExternalReferences.Add(new SpeciesExternalReference { Species = species, Provider = "gbif", DatasetIdentifier = Dataset, ExternalIdentifier = "42", CreatedAt = DateTime.UtcNow });
            db.Species.Add(species); return Task.FromResult(new CatalogCounts(1, 1));
        }, default));
        await using var observer = new ProfessorDbContext(options);
        Assert.Equal(16, await observer.Species.CountAsync()); Assert.Single(await observer.SpeciesExternalReferences.ToListAsync());
        Assert.Equal(0, (await observer.CatalogSyncRuns.FindAsync(id))!.RecordsProcessed);
    }
}
