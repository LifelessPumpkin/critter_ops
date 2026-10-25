using Microsoft.EntityFrameworkCore;
using skipper_api.Domain.Animals;

namespace HowellSync.Persistence;

// Contexts are short-lived and always use the session that owns the catalog lock.
public sealed class CatalogRunRepository
{
    public async Task<int> StartAsync(CatalogSession session, string provider, CatalogSyncOperation operation, DateTime started, CancellationToken token)
    {
        await using var db = session.CreateContext();
        // All Running rows use this catalog-wide lock; ownership proves they are abandoned.
        await db.CatalogSyncRuns.Where(x => x.Status == CatalogSyncStatus.Running)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, CatalogSyncStatus.Failed)
                .SetProperty(x => x.CompletedAt, DateTime.UtcNow)
                .SetProperty(x => x.ErrorMessage, "Interrupted execution recovered under the catalog lock; committed batches may remain."), token);
        var run = new CatalogSyncRun { Provider = provider, OperationType = operation,
            Status = CatalogSyncStatus.Running, StartedAt = started };
        db.CatalogSyncRuns.Add(run);
        await db.SaveChangesAsync(token);
        return run.Id;
    }

    public async Task<CatalogSyncStatus> FinishAsync(CatalogSession session, int runId, CatalogSyncStatus status, string? error, CancellationToken token)
    {
        await using var db = session.CreateContext();
        var run = await db.CatalogSyncRuns.SingleAsync(x => x.Id == runId, token);
        run.Status = status; run.CompletedAt = DateTime.UtcNow; run.ErrorMessage = error;
        if (status == CatalogSyncStatus.Completed && await db.CatalogSyncIssues.AnyAsync(x => x.CatalogSyncRunId == runId, token))
            run.Status = CatalogSyncStatus.CompletedWithIssues;
        await db.SaveChangesAsync(token);
        return run.Status;
    }
}

public sealed class CatalogSessionFactory
{
    public Task<CatalogSession?> TryOpenAsync(string connectionString, CancellationToken token) =>
        CatalogSession.TryOpenAsync(connectionString, token);
}
