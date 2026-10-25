using HowellSync.Commands;
using HowellSync.Persistence;
using HowellSync.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using skipper_api.Domain.Animals;

namespace HowellSync.Execution;

public sealed class CatalogOperationRunner(ProviderRegistry providers, IConfiguration configuration, ILogger<CatalogOperationRunner> logger,
    CatalogSessionFactory sessions, CatalogRunRepository history)
{
    public async Task<int> RunAsync(CatalogCommand command, CancellationToken token)
    {
        if (command.Name == "help") { Console.WriteLine(CatalogCommand.Help); return 0; }
        ICatalogProvider? provider;
        try { provider = command.Provider is null ? null : providers.Resolve(command.Provider, command.Operation); }
        catch (ArgumentException error) { logger.LogError("{ProviderError}", error.Message); return 2; }
        try { provider?.ValidateConfiguration(configuration.GetSection($"Providers:{provider.Name}")); }
        catch (Exception) { logger.LogError("Invalid selected provider configuration; check Providers settings"); return 2; }
        string connectionString;
        try
        {
            connectionString = configuration.GetConnectionString("ProfessorDb") ?? "";
            var settings = new NpgsqlConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(settings.Host) || string.IsNullOrWhiteSpace(settings.Database))
                throw new ArgumentException();
        }
        catch (Exception)
        {
            // Never expose configuration values, including connection-string parser errors.
            logger.LogError("Invalid ProfessorDb connection configuration; set ConnectionStrings__ProfessorDb");
            return 2;
        }
        var started = DateTime.UtcNow;
        try
        {
            token.ThrowIfCancellationRequested();
            await using var session = await sessions.TryOpenAsync(connectionString, token);
            if (session is null)
            {
                logger.LogWarning("A catalog operation is already running");
                return 3;
            }
            if (command.Name == "validate")
            {
                await ValidateAsync(session, token);
                logger.LogInformation("Professor validation completed: catalog tables are accessible; no data changed");
                return 0;
            }
            int? runId = null;
            if (!command.DryRun)
            {
                runId = await history.StartAsync(session, provider!.Name, command.Operation, started, token);
            }
            var context = new CatalogExecutionContext(session, runId, provider!.Name, command.DryRun);
            using var scope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["Operation"] = command.Name, ["Provider"] = provider.Name, ["ExecutionId"] = runId?.ToString() ?? Guid.NewGuid().ToString("N"),
                ["DryRun"] = command.DryRun, ["StartedAt"] = started
            });
            logger.LogInformation("Catalog operation started at {StartedAt}", started);
            var status = CatalogSyncStatus.Completed;
            var exitCode = 0;
            string? error = null;
            try
            {
                await provider.ExecuteAsync(command.Operation, context, token);
                token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                status = CatalogSyncStatus.Cancelled; exitCode = 130; error = "Operation cancelled; committed batches may remain.";
                logger.LogWarning("Catalog operation cancelled");
            }
            catch (Exception exception)
            {
                status = CatalogSyncStatus.Failed; exitCode = 1; error = "Operation failed; committed batches may remain. Inspect sanitized worker logs.";
                logger.LogError("Catalog operation failed ({FailureType})", exception.GetType().Name);
            }
            if (runId.HasValue)
            {
                try
                {
                    // Finalization must survive the operation's cancellation, but is bounded.
                    using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    status = await history.FinishAsync(session, runId.Value, status, error, cleanup.Token);
                }
                catch (Exception exception)
                {
                    logger.LogError("Execution history could not be finalized ({FailureType}); next non-dry operation will recover the incomplete run", exception.GetType().Name);
                    if (exitCode == 0) { exitCode = 1; status = CatalogSyncStatus.Failed; }
                }
            }
            logger.LogInformation("Catalog operation finished: {Status}, {CompletedAt}, {DurationMs}ms, processed {Processed}, created {Created}, updated {Updated}, skipped {Skipped}, failed {Failed}, exit {ExitCode}",
                status, DateTime.UtcNow, (DateTime.UtcNow - started).TotalMilliseconds, context.Counts.Processed, context.Counts.Created,
                context.Counts.Updated, context.Counts.Skipped, context.Counts.Failed, exitCode);
            return exitCode;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { logger.LogWarning("Catalog execution cancelled"); return 130; }
        catch (Exception exception)
        {
            logger.LogError("Catalog database/execution failure ({FailureType}); history may be unavailable", exception.GetType().Name);
            return 1;
        }
    }

    private static async Task ValidateAsync(CatalogSession session, CancellationToken token)
    {
        await using var db = session.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        await db.Database.ExecuteSqlRawAsync("SET TRANSACTION READ ONLY", token);
        // Bounded projections exercise mapped columns as well as table existence, including empty tables.
        await db.Species.AsNoTracking().Take(1).ToListAsync(token);
        await db.AnimalGroups.AsNoTracking().Take(1).ToListAsync(token);
        await db.AnimalVariants.AsNoTracking().Take(1).ToListAsync(token);
        await db.SpeciesExternalReferences.AsNoTracking().Take(1).ToListAsync(token);
        await db.CatalogSyncRuns.AsNoTracking().Take(1).ToListAsync(token);
        await db.CatalogSyncIssues.AsNoTracking().Take(1).ToListAsync(token);
        await transaction.RollbackAsync(token);
    }
}
