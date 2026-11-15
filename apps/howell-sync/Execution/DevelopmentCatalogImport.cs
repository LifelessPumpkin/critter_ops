using System.Text.Json;
using HowellSync.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using skipper_api.Data;

namespace HowellSync.Execution;

// Preflight and verification only. The existing runner owns catalog locks, HTTP, history and writes.
public sealed class DevelopmentCatalogImport(IConfiguration config, CatalogOperationRunner runner, ILogger logger)
{
    public static void ValidateTarget(string? environment, string connection)
    {
        var target = new NpgsqlConnectionStringBuilder(connection);
        if (environment != "Development" || target.Host is not ("localhost" or "127.0.0.1" or "::1") ||
            target.Database is null || !(target.Database.EndsWith("_dev") || target.Database.EndsWith("_test")))
            throw new ArgumentException("dev-import requires DOTNET_ENVIRONMENT=Development, a loopback host, and a database ending _dev or _test.");
    }
    public async Task<int> RunAsync(CatalogCommand command, CancellationToken token)
    {
        try
        {
            var connection = config.GetConnectionString("ProfessorDb") ?? "";
            ValidateTarget(config["DOTNET_ENVIRONMENT"], connection);
            var profile = DevelopmentCatalogProfile.Read(config, command.Profile ?? "demo");
            await using var db = new ProfessorDbContext(new DbContextOptionsBuilder<ProfessorDbContext>().UseNpgsql(connection).Options);
            if ((await db.Database.GetPendingMigrationsAsync(token)).Any())
            { logger.LogError("Apply Professor migrations before using development profiles."); return 2; }
            if (command.RetryRun is int retry)
            {
                var stored = await db.CatalogSyncRuns.AsNoTracking().Where(x => x.Id == retry).Select(x => x.Scope).SingleOrDefaultAsync(token);
                var original = stored is null ? null : JsonSerializer.Deserialize<CatalogCommand>(stored);
                if (original?.Name != "dev-import" || original.Profile != profile.Name || original.DevelopmentProfile is null)
                { logger.LogError("Retry requires a development run with the same profile and stored settings."); return 2; }
                profile = original.DevelopmentProfile;
                profile.Validate(command.Profile!);
            }
            var dataset = config["Providers:gbif:DatasetIdentifier"] ?? "d7dddbf4-2cf0-4f39-9b2a-bb099caae36c";
            if (command.Name == "verify-dev")
            {
                var count = await profile.SpeciesIds(db, dataset).CountAsync(token);
                var total = await db.Species.CountAsync(token);
                var references = await db.SpeciesExternalReferences.CountAsync(x => x.Provider == "gbif" && x.DatasetIdentifier == dataset, token);
                var synonyms = await db.SpeciesExternalReferences.CountAsync(x => x.Provider == "gbif" && x.DatasetIdentifier == dataset && x.ExternalTaxonomicStatus != "ACCEPTED", token);
                logger.LogInformation("Profile: {Profile}; distinct eligible Species: {Count}; total Species in Professor: {Total}; GBIF references: {References}; non-accepted references: {Synonyms}; status: {Status}. Verification creates no records; inspect run history for new/updated counts.", profile.Name, count, total, references, synonyms, count >= profile.MinimumSpecies ? "Completed" : "BelowMinimum");
                return count >= profile.MinimumSpecies ? 0 : 5;
            }
            command = command with { Profile = profile.Name, DevelopmentProfile = profile,
                Limit = command.RetryRun is null ? profile.MaximumExaminedRecords : null };
            return await runner.RunAsync(command, token);
        }
        catch (OperationCanceledException) { return 130; }
        catch (ArgumentException) { logger.LogError("Development configuration rejected; check environment, local target, selected profile and explicit bounds. No credentials logged."); return 2; }
        catch (Exception error) { logger.LogError("Development import failed ({Type}); check connectivity and migrations.", error.GetType().Name); return 1; }
    }
}
