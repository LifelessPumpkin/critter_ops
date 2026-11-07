using HowellSync.Commands;
using System.Text.Json;
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
        try { provider?.ValidateCommand(command); provider?.ValidateConfiguration(configuration.GetSection($"Providers:{provider.Name}")); }
        catch (Exception) { logger.LogError("Invalid selected provider configuration; check Providers settings, identifiable UserAgent, and bounded command scope"); return 2; }
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
            CatalogSyncRun? retry = null;
            int[] retryIds = [];
            if (command.RetryRun is int retryId)
            {
                await using var db = session.CreateContext();
                retry = await db.CatalogSyncRuns.AsNoTracking().SingleOrDefaultAsync(x => x.Id == retryId, token);
                if (retry is null || retry.Provider != provider!.Name || retry.OperationType != command.Operation ||
                    retry.DatasetIdentifier != provider.DatasetIdentifier || retry.Scope is null ||
                    retry.Status is not (CatalogSyncStatus.Failed or CatalogSyncStatus.Partial or CatalogSyncStatus.Cancelled or CatalogSyncStatus.Running))
                { logger.LogError("Retry requires a compatible incomplete run with stored scope and the same dataset"); return 2; }
                try
                {
                    var original = JsonSerializer.Deserialize<CatalogCommand>(retry.Scope)!;
                    if (original.Provider != command.Provider || original.Name != command.Name || original.RetryRun is not null || original.Limit is null or < 1 or > 1000)
                        throw new ArgumentException();
                    provider.ValidateCommand(original);
                    if (retry.ReferenceIds is not null)
                    {
                        retryIds = JsonSerializer.Deserialize<int[]>(retry.ReferenceIds) ?? throw new ArgumentException();
                        if (retryIds.Length > original.Limit || retryIds.Any(x => x <= 0) ||
                            !retryIds.SequenceEqual(retryIds.Distinct().Order()) ||
                            retry.LastCommittedReferenceId != 0 && !retryIds.Contains(retry.LastCommittedReferenceId))
                            throw new ArgumentException();
                    }
                    command = original with { DryRun = command.DryRun, RetryRun = retryId };
                }
                catch (Exception) { logger.LogError("Invalid stored retry scope"); return 2; }
            }
            else command = provider!.ResolveDefaults(command);
            int? runId = null;
            if (!command.DryRun)
            {
                runId = await history.StartAsync(session, provider!.Name, command.Operation, started, token, command, provider.DatasetIdentifier, retry);
            }
            var context = new CatalogExecutionContext(session, runId, provider!.Name, command.DryRun, command, logger);
            if (retry?.ReferenceIds is not null)
            {
                context.HasReferenceScope = true;
                context.ReferenceIds = retryIds;
                context.ResumeAfter = retry.LastCommittedReferenceId;
            }
            using var scope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["Operation"] = command.Name, ["Provider"] = provider.Name, ["ExecutionId"] = runId?.ToString() ?? Guid.NewGuid().ToString("N"),
                ["DryRun"] = command.DryRun, ["StartedAt"] = started
            });
            logger.LogInformation(new EventId(101, "START"), "{Operation} provider={Provider} mode={Mode} scope={Scope} limit={Limit} retry-of={RetryRun}",
                command.Name, provider.Name, command.DryRun ? "DRY RUN (no database writes)" : "WRITE",
                command.Query is not null ? "query: " + command.Query : command.Taxon is not null ? "taxon: " + command.Taxon : "existing references",
                command.Limit, command.RetryRun?.ToString() ?? "none");
            var status = CatalogSyncStatus.Completed;
            var exitCode = 0;
            string? error = null;
            var reason = "BoundedScopeComplete";
            try
            {
                await provider.ExecuteAsync(command.Operation, context, token);
                token.ThrowIfCancellationRequested();
            }
            catch (CatalogPartialException exception)
            {
                status = CatalogSyncStatus.Partial; exitCode = 4; reason = exception.Reason;
                error = "Request budget exhausted; committed work retained. Retry the stored scope.";
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                reason = "Cancelled";
                status = CatalogSyncStatus.Cancelled; exitCode = 130; error = "Operation cancelled; committed batches may remain.";
                logger.LogWarning("Catalog operation cancelled");
            }
            catch (Exception exception)
            {
                reason = "ExecutionFailure";
                status = CatalogSyncStatus.Failed; exitCode = 1; error = "Operation failed; committed batches may remain. Inspect sanitized worker logs.";
                logger.LogError("Catalog operation failed ({FailureType})", exception.GetType().Name);
            }
            if (runId.HasValue)
            {
                try
                {
                    // Finalization must survive the operation's cancellation, but is bounded.
                    using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    status = await history.FinishAsync(session, runId.Value, status, error, cleanup.Token, context.HttpRequests, context.HttpRetries, reason);
                }
                catch (Exception exception)
                {
                    logger.LogError("Execution history could not be finalized ({FailureType}); next non-dry operation will recover the incomplete run", exception.GetType().Name);
                    if (exitCode == 0) { exitCode = 1; status = CatalogSyncStatus.Failed; }
                }
            }
            logger.LogInformation(new EventId(102, "COUNTS"), "{Mode}: examined={Processed} created={Created} updated={Updated} unchanged={Unchanged} skipped={Skipped} failed={Failed} new-issues={Issues}",
                command.DryRun ? "Proposed" : "Committed", context.Counts.Processed, context.Counts.Created, context.Counts.Updated,
                context.Counts.Unchanged, context.Counts.Skipped, context.Counts.Failed + (status == CatalogSyncStatus.Failed ? 1 : 0), context.Counts.Issues);
            logger.Log(exitCode == 0 ? LogLevel.Information : exitCode == 1 ? LogLevel.Error : LogLevel.Warning, new EventId(103, "FINISH"), "{Status} | duration={DurationSeconds:F1}s requests={Requests} retries={Retries} | reason={Reason} exit={ExitCode}",
                status, (DateTime.UtcNow - started).TotalSeconds, context.HttpRequests, context.HttpRetries, reason, exitCode);
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
