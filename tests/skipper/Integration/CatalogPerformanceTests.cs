using System.Data.Common;
using System.Diagnostics;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using skipper_api.Controllers;
using skipper_api.Data;
using skipper_api.Dtos.Animals;
using Xunit;

namespace Skipper.Api.Tests.Data;

public class CatalogPerformanceTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    sealed class Capture : DbCommandInterceptor
    {
        public string Sql = "";
        public NpgsqlParameter[] Parameters = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData data, InterceptionResult<DbDataReader> result, CancellationToken ct = default)
        {
            Sql = command.CommandText;
            Parameters = command.Parameters.Cast<NpgsqlParameter>().Select(p => p.Clone()).ToArray();
            return ValueTask.FromResult(result);
        }
    }
    [Fact]
    public async Task MeasureProductionQueriesInDisposablePostgres()
    {
        if (Environment.GetEnvironmentVariable("CRITTEROPS_PERFORMANCE") != "1") return;
        var log = new StringBuilder();
        foreach (var size in new[] { 1000, 10000, 100000 })
        {
            var options = await fixture.CreateDatabaseAsync();
            await using var seed = new ProfessorDbContext(options);
            await seed.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Species" ("ScientificName", "CommonName", "AnimalGroupId", "IsActive", "TaxonomicStatus", "IsManuallyManaged")
                SELECT 'Fixture taxon ' || lpad(i::text,6,'0'), 'Fixture common ' || lpad(i::text,6,'0'), 1 + i % 6, true, 'Accepted', false FROM generate_series(1,{size}) i;
                INSERT INTO "SpeciesCommonNames" ("SpeciesId", "Name") SELECT "Id", 'Alternate ' || "ScientificName" FROM "Species" WHERE "ScientificName" LIKE 'Fixture%';
                INSERT INTO "SpeciesAliases" ("SpeciesId", "Name", "AliasType") SELECT "Id", 'Historical ' || "ScientificName", 'Historical' FROM "Species" WHERE "ScientificName" LIKE 'Fixture%';
                INSERT INTO "AnimalVariants" ("SpeciesId", "Name", "VariantType", "IsActive") SELECT 4, 'Fixture variant ' || lpad(i::text,6,'0'), 'Morph', true FROM generate_series(1,{size}) i;
                ANALYZE;
                """);
            if (Environment.GetEnvironmentVariable("CRITTEROPS_TRIGRAM_NAMES") == "1")
                await seed.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_SpeciesCommonNames_Name\" ON \"SpeciesCommonNames\" USING gin (\"Name\" gin_trgm_ops); CREATE INDEX IF NOT EXISTS \"IX_SpeciesAliases_Name\" ON \"SpeciesAliases\" USING gin (\"Name\" gin_trgm_ops); ANALYZE;");
            var capture = new Capture();
            await using var db = new ProfessorDbContext(new DbContextOptionsBuilder<ProfessorDbContext>().UseNpgsql(seed.Database.GetConnectionString()).AddInterceptors(capture).Options);
            var api = new AnimalCatalogController(db);
            foreach (var (name, request) in new (string, CatalogSearchRequestDto)[] {
                ("exact", new() { Search = "Fixture taxon 000500" }), ("partial", new() { Search = "taxon 0005" }),
                ("common", new() { Search = "common 0005" }), ("alternate", new() { Search = "Alternate Fixture taxon 0005" }),
                ("alias", new() { Search = "Historical Fixture taxon 0005" }), ("category", new() { AnimalGroupId = 3 }),
                ("combined", new() { Search = "taxon 0005", AnimalGroupId = 3 }), ("empty", new()),
                ("no-match", new() { Search = "absent-zzzz" }), ("offset", new() { Page = 1000 }),
                ("variant", new() { Search = "variant 0005", SpeciesId = 4 }) })
            {
                var timer = Stopwatch.StartNew();
                if (name == "variant") await api.Variants(request, default); else await api.Species(request, default);
                timer.Stop();
                log.AppendLine($"Dataset={size} Query={name} API-ms={timer.Elapsed.TotalMilliseconds:F2}");
                await using var connection = new NpgsqlConnection(seed.Database.GetConnectionString());
                await connection.OpenAsync();
                await using var explain = new NpgsqlCommand("EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT) " + capture.Sql, connection);
                explain.Parameters.AddRange(capture.Parameters);
                log.AppendLine(capture.Sql);
                await using var reader = await explain.ExecuteReaderAsync();
                while (await reader.ReadAsync()) log.AppendLine(reader.GetString(0));
            }
        }
        await File.WriteAllTextAsync(Environment.GetEnvironmentVariable("CRITTEROPS_PERFORMANCE_OUTPUT") ?? "/tmp/critterops-performance.txt", log.ToString());
    }
}
