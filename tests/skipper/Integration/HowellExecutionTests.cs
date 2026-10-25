using HowellSync.Commands;
using HowellSync.Execution;
using HowellSync.Persistence;
using HowellSync.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using skipper_api.Data;
using skipper_api.Domain.Animals;
using Skipper.Api.Tests.Data;
using Xunit;

namespace MaryAnn.Howell;

public class HowellExecutionTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static CatalogOperationRunner Runner(string connection, Func<CatalogSyncOperation, CatalogExecutionContext, CancellationToken, Task> execute) =>
        new(new ProviderRegistry([new TestProvider(execute)]), new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string,string?> { ["ConnectionStrings:ProfessorDb"] = connection }).Build(), NullLogger<CatalogOperationRunner>.Instance, new HowellSync.Persistence.CatalogSessionFactory(), new HowellSync.Persistence.CatalogRunRepository());

    private async Task<(DbContextOptions<ProfessorDbContext> Options, string Connection)> Database()
    {
        var options = await fixture.CreateDatabaseAsync();
        using var db = new ProfessorDbContext(options);
        return (options, db.Database.GetConnectionString()!);
    }

    [Theory]
    [InlineData("import", CatalogSyncOperation.Import)]
    [InlineData("sync", CatalogSyncOperation.Synchronization)]
    public async Task HistoryProgressAndIssues(string command, CatalogSyncOperation operation)
    {
        var (options, connection) = await Database();
        var runner = Runner(connection, async (actual, context, token) =>
        {
            Assert.Equal(operation, actual);
            await using var observer = new ProfessorDbContext(options);
            Assert.Equal(CatalogSyncStatus.Running, (await observer.CatalogSyncRuns.SingleAsync()).Status);
            await context.BatchAsync((db, _) => {
                db.Species.Add(new Species { ScientificName = "Test species", AnimalGroupId = 1 });
                return Task.FromResult(new CatalogCounts(1, 1));
            }, token);
            await context.RecordIssueAsync("test", "Controlled test diagnostic", CatalogIssueSeverity.Warning, token);
        });
        Assert.Equal(0, await runner.RunAsync(new(command, "test"), default));
        await using var db = new ProfessorDbContext(options);
        var run = await db.CatalogSyncRuns.SingleAsync();
        Assert.Equal("test", run.Provider); Assert.Equal(operation, run.OperationType);
        Assert.Equal(CatalogSyncStatus.CompletedWithIssues, run.Status);
        Assert.NotNull(run.StartedAt); Assert.True(run.CompletedAt >= run.StartedAt);
        Assert.Equal(1, run.RecordsCreated); Assert.Equal(1, run.RecordsProcessed);
        Assert.Equal(run.Id, (await db.CatalogSyncIssues.SingleAsync()).CatalogSyncRunId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedOrCancelledBatchRollsBackAndReleasesLock(bool cancel)
    {
        var (options, connection) = await Database();
        using var source = new CancellationTokenSource();
        var runner = Runner(connection, async (_, context, token) => {
            await context.BatchAsync(async (db, ct) => {
                db.Species.Add(new Species { ScientificName = "Rollback specimen", AnimalGroupId = 1 });
                await db.SaveChangesAsync(ct);
                if (cancel) { source.Cancel(); ct.ThrowIfCancellationRequested(); }
                throw new InvalidOperationException("secret must not be persisted");
            }, token);
        });
        Assert.Equal(cancel ? 130 : 1, await runner.RunAsync(new("import", "test"), source.Token));
        await using var db = new ProfessorDbContext(options);
        Assert.False(await db.Species.AnyAsync(x => x.ScientificName == "Rollback specimen"));
        var run = await db.CatalogSyncRuns.SingleAsync();
        Assert.Equal(cancel ? CatalogSyncStatus.Cancelled : CatalogSyncStatus.Failed, run.Status);
        Assert.NotNull(run.CompletedAt); Assert.Equal(0, run.RecordsCreated);
        Assert.DoesNotContain("secret", run.ErrorMessage!);
        Assert.Equal(0, await Runner(connection, (_, _, _) => Task.CompletedTask).RunAsync(new("sync", "test"), default));
    }

    [Fact]
    public async Task IndependentWorkersContendAndDoNotRecoverActiveRun()
    {
        var (options, connection) = await Database();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = Runner(connection, async (_, _, token) => { entered.SetResult(); await release.Task.WaitAsync(token); });
        var pending = first.RunAsync(new("import", "test"), default);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(20));
            Assert.Equal(3, await Runner(connection, (_, _, _) => Task.CompletedTask).RunAsync(new("sync", "test"), default));
            await using var db = new ProfessorDbContext(options);
            Assert.Equal(CatalogSyncStatus.Running, (await db.CatalogSyncRuns.SingleAsync()).Status);
        }
        finally { release.TrySetResult(); }
        Assert.Equal(0, await pending);
        Assert.Equal(0, await Runner(connection, (_, _, _) => Task.CompletedTask).RunAsync(new("sync", "test"), default));
    }

    [Fact]
    public async Task RecoversAbandonedRunOnlyOnWritingOperation()
    {
        var (options, connection) = await Database();
        await using var db = new ProfessorDbContext(options);
        db.CatalogSyncRuns.Add(new CatalogSyncRun { Provider = "test", OperationType = CatalogSyncOperation.Import, Status = CatalogSyncStatus.Running, StartedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var runner = Runner(connection, (_, _, _) => Task.CompletedTask);
        Assert.Equal(0, await runner.RunAsync(new("validate"), default));
        Assert.Equal(0, await runner.RunAsync(new("import", "test", true), default));
        db.ChangeTracker.Clear();
        Assert.Equal(CatalogSyncStatus.Running, (await db.CatalogSyncRuns.SingleAsync()).Status);
        Assert.Equal(0, await runner.RunAsync(new("import", "test"), default));
        db.ChangeTracker.Clear();
        var runs = await db.CatalogSyncRuns.OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(CatalogSyncStatus.Failed, runs[0].Status); Assert.NotNull(runs[0].CompletedAt);
        Assert.Contains("Interrupted", runs[0].ErrorMessage!); Assert.Equal(CatalogSyncStatus.Completed, runs[1].Status);
    }

    [Fact]
    public async Task DryRunPlansWithoutHistoryOrIssuesAndDatabaseRejectsWrites()
    {
        var (options, connection) = await Database();
        var runner = Runner(connection, async (_, context, token) => {
            Assert.True(context.DryRun); Assert.Null(context.RunId);
            await context.BatchAsync(async (db, ct) => { await db.Species.Take(1).ToListAsync(ct); return new CatalogCounts(1, 1); }, token);
            await context.RecordIssueAsync("test", "Dry diagnostic", CatalogIssueSeverity.Warning, token);
        });
        Assert.Equal(0, await runner.RunAsync(new("import", "test", true), default));
        var writer = Runner(connection, async (_, context, token) => {
            await context.BatchAsync(async (db, ct) => {
                await db.Database.ExecuteSqlRawAsync("DELETE FROM \"Species\"", ct);
                return new CatalogCounts();
            }, token);
        });
        Assert.Equal(1, await writer.RunAsync(new("import", "test", true), default));
        await using var db = new ProfessorDbContext(options);
        Assert.Empty(await db.CatalogSyncRuns.ToListAsync()); Assert.Empty(await db.CatalogSyncIssues.ToListAsync());
        Assert.True(await db.Species.AnyAsync());
    }

    [Fact]
    public async Task LostLockConnectionCannotReconnectAndNewWorkerRecovers()
    {
        var (options, connection) = await Database();
        Exception? reconnectError = null;
        var runner = Runner(connection, async (_, context, token) => {
            await context.BatchAsync(async (db, ct) => {
                var pid = ((NpgsqlConnection)db.Database.GetDbConnection()).ProcessID;
                await using var killer = new NpgsqlConnection(connection); await killer.OpenAsync(ct);
                await using var command = new NpgsqlCommand("SELECT pg_terminate_backend(@pid)", killer);
                command.Parameters.AddWithValue("pid", pid); await command.ExecuteScalarAsync(ct);
                await Record.ExceptionAsync(() => db.Species.AnyAsync(ct));
                reconnectError = await Record.ExceptionAsync(() => db.Species.AnyAsync(ct));
                throw new InvalidOperationException("Lost lock session must abort the batch");
            }, token);
        });
        Assert.Equal(1, await runner.RunAsync(new("import", "test"), default));
        Assert.IsType<InvalidOperationException>(reconnectError);
        Assert.Contains("cannot be reopened", reconnectError.Message);
        Assert.Equal(0, await Runner(connection, (_, _, _) => Task.CompletedTask).RunAsync(new("sync", "test"), default));
        await using var db = new ProfessorDbContext(options);
        var runs = await db.CatalogSyncRuns.OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(CatalogSyncStatus.Failed, runs[0].Status); Assert.Equal(CatalogSyncStatus.Completed, runs[1].Status);
    }

    [Fact]
    public async Task KilledWorkerProcessRollsBackAndReleasesSessionLock()
    {
        var (options, connection) = await Database();
        var signal = Path.Combine(Path.GetTempPath(), "howell-" + Guid.NewGuid().ToString("N"));
        var start = new System.Diagnostics.ProcessStartInfo("dotnet") { UseShellExecute = false };
        start.ArgumentList.Add(typeof(HowellProcessHarness).Assembly.Location);
        start.ArgumentList.Add("--howell-test-child"); start.ArgumentList.Add(signal);
        start.Environment["HOWELL_TEST_ConnectionStrings__ProfessorDb"] = connection;
        using var process = System.Diagnostics.Process.Start(start)!;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            while (!File.Exists(signal))
            {
                Assert.False(process.HasExited, "Test worker exited before holding its transaction");
                await Task.Delay(50, timeout.Token);
            }
            Assert.Equal(3, await Runner(connection, (_, _, _) => Task.CompletedTask).RunAsync(new("import", "test"), default));
            process.Kill(); await process.WaitForExitAsync(timeout.Token);
            Assert.Equal(0, await Runner(connection, (_, _, _) => Task.CompletedTask).RunAsync(new("sync", "test"), timeout.Token));
            await using var db = new ProfessorDbContext(options);
            Assert.False(await db.Species.AnyAsync(x => x.ScientificName == "Crash rollback specimen"));
            var runs = await db.CatalogSyncRuns.OrderBy(x => x.Id).ToListAsync();
            Assert.Equal(CatalogSyncStatus.Failed, runs[0].Status);
            Assert.Equal(CatalogSyncStatus.Completed, runs[1].Status);
        }
        finally
        {
            if (!process.HasExited) { process.Kill(); await process.WaitForExitAsync(); }
            if (File.Exists(signal)) File.Delete(signal);
        }
    }

    private sealed class TestProvider(Func<CatalogSyncOperation, CatalogExecutionContext, CancellationToken, Task> execute) : ICatalogProvider
    {
        public string Name => "test";
        public IReadOnlySet<CatalogSyncOperation> SupportedOperations { get; } = new HashSet<CatalogSyncOperation> { CatalogSyncOperation.Import, CatalogSyncOperation.Synchronization };
        public void ValidateConfiguration(IConfigurationSection configuration) { }
        public Task ExecuteAsync(CatalogSyncOperation operation, CatalogExecutionContext context, CancellationToken token) => execute(operation, context, token);
    }
}
