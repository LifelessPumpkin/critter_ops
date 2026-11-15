using HowellSync.Commands;
using HowellSync.Execution;
using HowellSync.Providers.Gbif;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using skipper_api.Data;
using Xunit;

namespace MaryAnn.Howell;
public partial class GbifImportTests
{
    private static IConfiguration DevConfig(string connection, int minimum = 100, int budget = 800) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
        ["DOTNET_ENVIRONMENT"] = "Development", ["ConnectionStrings:ProfessorDb"] = connection,
        ["Providers:gbif:UserAgent"] = "CritterOps/1.0 (+https://critterops.test/contact)",
        ["DevelopmentCatalog:Profiles:demo:Name"] = "demo",
        ["DevelopmentCatalog:Profiles:demo:Genera:0"] = "Novelus",
        ["DevelopmentCatalog:Profiles:demo:MinimumSpecies"] = minimum.ToString(),
        ["DevelopmentCatalog:Profiles:demo:TargetSpecies"] = minimum.ToString(),
        ["DevelopmentCatalog:Profiles:demo:MaximumSpecies"] = "200",
        ["DevelopmentCatalog:Profiles:demo:MaximumExaminedRecords"] = "1000",
        ["DevelopmentCatalog:Profiles:demo:Provider:MaximumRequestsPerRun"] = budget.ToString(),
        ["DevelopmentCatalog:Profiles:demo:Provider:MinimumRequestInterval"] = "00:00:00",
        ["DevelopmentCatalog:Profiles:demo:Provider:MaximumConcurrentRequests"] = "1",
        ["DevelopmentCatalog:Profiles:demo:Provider:PageSize"] = "100",
        ["DevelopmentCatalog:Profiles:demo:Provider:RequestTimeout"] = "00:00:02",
    }).Build();

    [Fact]
    public async Task DevelopmentWorkflowCountsPersistsAndRerunsWithoutDuplicates()
    {
        var (options, connection) = await Database();
        using var handler = new Handler((request, _) => {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/search"))
            {
                Assert.Contains("rank=SPECIES&status=ACCEPTED", request.RequestUri.Query);
                return Task.FromResult(Json(new GbifPage<GbifTaxon>(0, 100, true, Enumerable.Range(1000, 100).Select(i => Taxon(i, "Novelus " + i)).ToList())));
            }
            if (path.EndsWith("vernacularNames")) return Task.FromResult(Json(new GbifPage<GbifVernacularName>(0, 100, true, [new("Alternate animal", "eng", null)])));
            var key = int.Parse(path.Split('/').Last());
            return Task.FromResult(Json(Taxon(key, "Novelus " + key)));
        });
        var wrapper = new DevelopmentCatalogImport(DevConfig(connection), Runner(connection, handler, 250, 100), NullLogger.Instance);
        var command = new CatalogCommand("dev-import", "gbif", Profile: "demo");
        Assert.Equal(0, await wrapper.RunAsync(command with { DryRun = true }, default));
        await using var db = new ProfessorDbContext(options);
        Assert.Empty(await db.SpeciesExternalReferences.ToListAsync());
        Assert.Empty(await db.CatalogSyncRuns.ToListAsync());
        Assert.Equal(0, await wrapper.RunAsync(command, default));
        var ids = await db.SpeciesExternalReferences.OrderBy(x => x.Id).Select(x => x.SpeciesId).ToArrayAsync();
        Assert.Equal(100, ids.Distinct().Count());
        Assert.Equal(0, await wrapper.RunAsync(command, default));
        Assert.Equal(ids, await db.SpeciesExternalReferences.OrderBy(x => x.Id).Select(x => x.SpeciesId).ToArrayAsync());
        Assert.Equal(100, await db.SpeciesCommonNames.CountAsync());
        Assert.Equal(2, await db.CatalogSyncRuns.CountAsync());
        var tooFew = new DevelopmentCatalogImport(DevConfig(connection, 101), Runner(connection, handler, 250, 100), NullLogger.Instance);
        Assert.Equal(5, await tooFew.RunAsync(command, default));
    }
    [Fact]
    public async Task DevelopmentWorkflowReportsBudgetAndUnavailableProvider()
    {
        var (_, connection) = await Database();
        using var handler = new Handler((request, _) => Task.FromResult(Json(new GbifPage<GbifTaxon>(0, 100, false, [Taxon()]))));
        var wrapper = new DevelopmentCatalogImport(DevConfig(connection, budget: 1), Runner(connection, handler, 1, 1), NullLogger.Instance);
        Assert.Equal(4, await wrapper.RunAsync(new("dev-import", "gbif", Profile: "demo"), default));
        using var unavailable = new Handler((_, _) => Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)));
        wrapper = new(DevConfig(connection, budget: 1), Runner(connection, unavailable, 5), NullLogger.Instance);
        Assert.Equal(1, await wrapper.RunAsync(new("dev-import", "gbif", Profile: "demo"), default));
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        Assert.Equal(130, await wrapper.RunAsync(new("dev-import", "gbif"), cancel.Token));
        wrapper = new(DevConfig("Host=localhost;Port=1;Database=missing_test;Timeout=1"), Runner(connection, handler), NullLogger.Instance);
        Assert.Equal(1, await wrapper.RunAsync(new("dev-import", "gbif"), default));
    }
    [Fact]
    public async Task ExpandedBuildsOnDemoAndProfilesExcludeUnrelatedRecords()
    {
        var (options, connection) = await Database();
        using var handler = new Handler((request, _) => {
            var uri = request.RequestUri!;
            if (uri.AbsolutePath.EndsWith("/search"))
            {
                var query = uri.Query.TrimStart('?').Split('&').Select(x => x.Split('=')).ToDictionary(x => x[0], x => x[1]);
                var offset = int.Parse(query["offset"]); var limit = int.Parse(query["limit"]);
                var results = Enumerable.Range(1000 + offset, Math.Min(limit, 1000 - offset)).Select(i => Taxon(i, "Novelus " + i)).ToList();
                return Task.FromResult(Json(new GbifPage<GbifTaxon>(offset, limit, offset + results.Count >= 1000, results)));
            }
            if (uri.AbsolutePath.EndsWith("vernacularNames")) return Task.FromResult(Json(new GbifPage<GbifVernacularName>(0, 100, true, [])));
            var key = int.Parse(uri.AbsolutePath.Split('/').Last());
            return Task.FromResult(Json(Taxon(key, "Novelus " + key)));
        });
        var demoConfig = DevConfig(connection);
        var settings = demoConfig.AsEnumerable().ToDictionary(x => x.Key, x => x.Value);
        foreach (var item in demoConfig.GetSection("DevelopmentCatalog:Profiles:demo").AsEnumerable(true))
            settings["DevelopmentCatalog:Profiles:expanded:" + item.Key] = item.Value;
        settings["DevelopmentCatalog:Profiles:expanded:Name"] = "expanded";
        settings["DevelopmentCatalog:Profiles:expanded:MinimumSpecies"] = "1000";
        settings["DevelopmentCatalog:Profiles:expanded:TargetSpecies"] = "1000";
        settings["DevelopmentCatalog:Profiles:expanded:MaximumSpecies"] = "1200";
        settings["DevelopmentCatalog:Profiles:expanded:MaximumExaminedRecords"] = "2000";
        settings["DevelopmentCatalog:Profiles:expanded:Provider:MaximumRequestsPerRun"] = "4000";
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var wrapper = new DevelopmentCatalogImport(config, Runner(connection, handler, 4000, 100), NullLogger.Instance);
        Assert.Equal(0, await wrapper.RunAsync(new("dev-import", "gbif", Profile: "demo"), default));
        await using var db = new ProfessorDbContext(options);
        var demoIds = await db.SpeciesExternalReferences.OrderBy(x => x.Id).Select(x => new { x.Id, x.SpeciesId, x.ExternalIdentifier }).ToArrayAsync();
        Assert.Equal(100, demoIds.Length);
        Assert.Equal(0, await wrapper.RunAsync(new("dev-import", "gbif", Profile: "expanded"), default));
        Assert.Equal(1000, await DevelopmentCatalogProfile.Read(config, "expanded").SpeciesIds(db, Dataset).CountAsync());
        Assert.Equal(demoIds, await db.SpeciesExternalReferences.OrderBy(x => x.Id).Take(100).Select(x => new { x.Id, x.SpeciesId, x.ExternalIdentifier }).ToArrayAsync());
        Assert.Equal(0, await wrapper.RunAsync(new("dev-import", "gbif", Profile: "expanded"), default));
        Assert.Equal(0, await wrapper.RunAsync(new("dev-import", "gbif", Profile: "demo"), default));
        Assert.Equal(1000, await db.SpeciesExternalReferences.CountAsync());
        var runs = await db.CatalogSyncRuns.OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(new[] { 100, 900, 0, 0 }, runs.Select(x => x.RecordsCreated));
        Assert.Equal(0, runs.Last().HttpRequests);
        Assert.Equal(0, await wrapper.RunAsync(new("verify-dev", "gbif", Profile: "expanded"), default));
        var unrelated = DevelopmentCatalogProfile.Read(config, "demo") with { Genera = ["Unrelated"] };
        Assert.Equal(0, await unrelated.SpeciesIds(db, Dataset).CountAsync());
        Assert.Equal(0, await unrelated.SpeciesIds(db, "another-dataset").CountAsync());
    }

    [Fact]
    public async Task PartialProfileRetryReplaysCommittedRecordsAndRetainsHistory()
    {
        var (options, connection) = await Database();
        var fail = true;
        using var handler = new Handler((request, _) => {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/search")) return Task.FromResult(Json(new GbifPage<GbifTaxon>(0, 100, true, Enumerable.Range(1000, 100).Select(i => Taxon(i, "Novelus " + i)).ToList())));
            if (fail && path.EndsWith("/1001")) return Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable));
            if (path.EndsWith("vernacularNames")) return Task.FromResult(Json(new GbifPage<GbifVernacularName>(0, 100, true, [])));
            var key = int.Parse(path.Split('/').Last()); return Task.FromResult(Json(Taxon(key, "Novelus " + key)));
        });
        var wrapper = new DevelopmentCatalogImport(DevConfig(connection), Runner(connection, handler, 800, 100), NullLogger.Instance);
        Assert.Equal(1, await wrapper.RunAsync(new("dev-import", "gbif", Profile: "demo"), default));
        await using var db = new ProfessorDbContext(options);
        var saved = await db.SpeciesExternalReferences.SingleAsync();
        var runId = await db.CatalogSyncRuns.Select(x => x.Id).SingleAsync();
        fail = false;
        Assert.Equal(0, await wrapper.RunAsync(new("dev-import", "gbif", RetryRun: runId, Profile: "demo"), default));
        Assert.Equal(100, await db.SpeciesExternalReferences.CountAsync());
        Assert.Equal(saved.SpeciesId, await db.SpeciesExternalReferences.Where(x => x.Id == saved.Id).Select(x => x.SpeciesId).SingleAsync());
        Assert.Equal(runId, await db.CatalogSyncRuns.OrderByDescending(x => x.Id).Select(x => x.RetryOfRunId).FirstAsync());
    }

}
