using HowellSync.Persistence;
using HowellSync.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using skipper_api.Data;
using skipper_api.Domain.Animals;

namespace HowellSync.Execution;

public sealed class CatalogPartialException(string reason) : Exception(reason)
{
    public string Reason { get; } = reason;
}

public sealed record CatalogCounts(int Processed = 0, int Created = 0, int Updated = 0, int Skipped = 0, int Failed = 0, int Unchanged = 0, int Issues = 0);

public sealed class CatalogExecutionContext(CatalogSession session, int? runId, string provider, bool dryRun, CatalogCommand? command = null, ILogger? logger = null)
{
    public CatalogCommand Command { get; } = command ?? new("import", provider, dryRun);
    public bool DryRun => dryRun;
    public int? RunId => runId;
    public string? CurrentExternalIdentifier { get; set; }
    public bool HasReferenceScope { get; set; }
    public int HttpRequests { get; set; }
    public int HttpRetries { get; set; }
    public int[] ReferenceIds { get; set; } = [];
    public int LastCommittedReferenceId { get; private set; }
    public int ResumeAfter { get; set; }
    public Task<T> ReadAsync<T>(Func<ProfessorDbContext, Task<T>> read)
    {
        return ReadCoreAsync(read);
    }
    private async Task<T> ReadCoreAsync<T>(Func<ProfessorDbContext, Task<T>> read)
    {
        await using var db = session.CreateContext();
        return await read(db);
    }
    public CatalogCounts Counts { get; private set; } = new();

    // Fetch external data BEFORE entering this callback. Calls must be sequential and bounded.
    // Dry-run callbacks must query/plan only: PostgreSQL rejects even raw SQL mutations.
    public async Task BatchAsync(Func<ProfessorDbContext, CancellationToken, Task<CatalogCounts>> apply, CancellationToken token, int? checkpoint = null)
    {
        await using var db = session.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        if (dryRun) await db.Database.ExecuteSqlRawAsync("SET TRANSACTION READ ONLY", token);
        var counts = await apply(db, token);
        var newIssues = db.ChangeTracker.Entries<CatalogSyncIssue>().Where(x => x.State == EntityState.Added).Select(x => x.Entity).ToArray();
        counts = counts with { Issues = counts.Issues + newIssues.Length };
        if (new[] { counts.Processed, counts.Created, counts.Updated, counts.Skipped, counts.Failed, counts.Unchanged, counts.Issues }.Any(x => x < 0))
            throw new InvalidOperationException("Batch counts must be nonnegative.");
        var total = new CatalogCounts(checked(Counts.Processed + counts.Processed), checked(Counts.Created + counts.Created),
            checked(Counts.Updated + counts.Updated), checked(Counts.Skipped + counts.Skipped), checked(Counts.Failed + counts.Failed), checked(Counts.Unchanged + counts.Unchanged), checked(Counts.Issues + counts.Issues));
        if (!dryRun)
        {
            // Progress and catalog writes commit atomically for each batch.
            var run = await db.CatalogSyncRuns.SingleAsync(x => x.Id == runId, token);
            ApplyCounts(run, total);
            run.HttpRequests = HttpRequests; run.HttpRetries = HttpRetries;
            if (checkpoint.HasValue) run.LastCommittedReferenceId = checkpoint.Value;
            await db.SaveChangesAsync(token);
            token.ThrowIfCancellationRequested();
            await transaction.CommitAsync(token);
        }
        else await transaction.RollbackAsync(token);
        foreach (var issue in newIssues)
            logger?.LogWarning(new EventId(106, "ISSUE"), "{Mode} {IssueType} provider={Provider} external-id={Identifier}: {Description}",
                dryRun ? "Proposed" : "Recorded", issue.IssueType, issue.Provider, issue.ExternalIdentifier ?? "none", issue.Description);
        Counts = total;
        if (checkpoint.HasValue) LastCommittedReferenceId = checkpoint.Value;
    }

    public Task RecordIssueAsync(string issueType, string safeDescription, CatalogIssueSeverity severity, CancellationToken token, string? datasetIdentifier = null, string? externalIdentifier = null) =>
        BatchAsync((db, _) =>
        {
            Synchronization.CatalogIssueRecorder.Add(db, runId, provider, datasetIdentifier, externalIdentifier,
                issueType, safeDescription, severity: severity);
            return Task.FromResult(new CatalogCounts());
        }, token);

    internal static void ApplyCounts(CatalogSyncRun run, CatalogCounts counts)
    {
        run.RecordsProcessed = counts.Processed; run.RecordsCreated = counts.Created;
        run.RecordsUpdated = counts.Updated; run.RecordsSkipped = counts.Skipped; run.RecordsFailed = counts.Failed;
        run.RecordsUnchanged = counts.Unchanged; run.IssuesGenerated = counts.Issues;
    }
}
