using HowellSync.Persistence;
using HowellSync.Commands;
using Microsoft.EntityFrameworkCore;
using skipper_api.Data;
using skipper_api.Domain.Animals;

namespace HowellSync.Execution;

public sealed record CatalogCounts(int Processed = 0, int Created = 0, int Updated = 0, int Skipped = 0, int Failed = 0);

public sealed class CatalogExecutionContext(CatalogSession session, int? runId, string provider, bool dryRun, CatalogCommand? command = null)
{
    public CatalogCommand Command { get; } = command ?? new("import", provider, dryRun);
    public bool DryRun => dryRun;
    public int? RunId => runId;
    public CatalogCounts Counts { get; private set; } = new();

    // Fetch external data BEFORE entering this callback. Calls must be sequential and bounded.
    // Dry-run callbacks must query/plan only: PostgreSQL rejects even raw SQL mutations.
    public async Task BatchAsync(Func<ProfessorDbContext, CancellationToken, Task<CatalogCounts>> apply, CancellationToken token)
    {
        await using var db = session.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        if (dryRun) await db.Database.ExecuteSqlRawAsync("SET TRANSACTION READ ONLY", token);
        var counts = await apply(db, token);
        if (new[] { counts.Processed, counts.Created, counts.Updated, counts.Skipped, counts.Failed }.Any(x => x < 0))
            throw new InvalidOperationException("Batch counts must be nonnegative.");
        var total = new CatalogCounts(checked(Counts.Processed + counts.Processed), checked(Counts.Created + counts.Created),
            checked(Counts.Updated + counts.Updated), checked(Counts.Skipped + counts.Skipped), checked(Counts.Failed + counts.Failed));
        if (!dryRun)
        {
            // Progress and catalog writes commit atomically for each batch.
            var run = await db.CatalogSyncRuns.SingleAsync(x => x.Id == runId, token);
            ApplyCounts(run, total);
            await db.SaveChangesAsync(token);
            token.ThrowIfCancellationRequested();
            await transaction.CommitAsync(token);
        }
        else await transaction.RollbackAsync(token);
        Counts = total;
    }

    public Task RecordIssueAsync(string issueType, string safeDescription, CatalogIssueSeverity severity, CancellationToken token, string? datasetIdentifier = null, string? externalIdentifier = null) =>
        BatchAsync((db, _) =>
        {
            // Descriptions are provider-owned diagnostics, never raw external records or exception text.
            if (!dryRun) db.CatalogSyncIssues.Add(new CatalogSyncIssue
            {
                CatalogSyncRunId = runId, Provider = provider, IssueType = issueType,
                Description = safeDescription, Severity = severity, CreatedAt = DateTime.UtcNow,
                DatasetIdentifier = datasetIdentifier, ExternalIdentifier = externalIdentifier
            });
            return Task.FromResult(new CatalogCounts());
        }, token);

    internal static void ApplyCounts(CatalogSyncRun run, CatalogCounts counts)
    {
        run.RecordsProcessed = counts.Processed; run.RecordsCreated = counts.Created;
        run.RecordsUpdated = counts.Updated; run.RecordsSkipped = counts.Skipped; run.RecordsFailed = counts.Failed;
    }
}
