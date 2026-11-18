using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using skipper_api.Data;
using RecurrenceType = skipper_api.Domain.Tasks.RecurrenceType;
using TaskType = skipper_api.Domain.Tasks.TaskType;
using skipper_api.Dtos.Tasks;
using skipper_api.Services.Tasks;
using Skipper.Api.Tests.Data;
using Xunit;
using TaskEntity = skipper_api.Domain.Tasks.Task;

namespace Skipper.Api.Tests;

public sealed class TaskCompletionConcurrencyTests(PostgresFixture database) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData(RecurrenceType.None)]
    [InlineData(RecurrenceType.Daily)]
    public async Task CompetingCompletions_ReturnFirstCommittedMetadataAndOneSetOfSideEffects(RecurrenceType recurrence)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var token = timeout.Token;
        var options = await database.CreateDatabaseAsync();
        var original = await SeedAsync(options, recurrence);
        var gate = new BeforeCommitGate();
        await using var firstContext = new ProfessorDbContext(new DbContextOptionsBuilder<ProfessorDbContext>(options)
            .AddInterceptors(gate).Options);
        await using var secondContext = new ProfessorDbContext(options);
        // Deliberately retain stale state to exercise EF identity resolution after lock acquisition.
        await secondContext.Tasks.SingleAsync(task => task.Id == original.Id, token);
        await secondContext.Database.OpenConnectionAsync(token);
        var secondPid = ((NpgsqlConnection)secondContext.Database.GetDbConnection()).ProcessID;

        var first = new TaskService(firstContext).CompleteAsync(original.Id,
            new CompleteTaskRequestDto { CompletionNotes = "First notes", CompletedBy = "First keeper" }, token);
        await gate.Saved.Task.WaitAsync(token);
        var second = new TaskService(secondContext).CompleteAsync(original.Id,
            new CompleteTaskRequestDto { CompletionNotes = "Later notes", CompletedBy = "Later keeper" }, token);
        try
        {
            await WaitForLockAsync(options, secondPid, token);
        }
        finally
        {
            gate.Release.TrySetResult();
        }
        var results = await Task.WhenAll(first, second).WaitAsync(token);
        Assert.All(results, result => Assert.Equal(CompleteTaskResultStatus.Completed, result.Status));
        Assert.Equal(results[0].Task!.CompletedAt, results[1].Task!.CompletedAt);
        Assert.All(results, result => Assert.Equal("First notes", result.Task!.CompletionNotes));

        await using var retryContext = new ProfessorDbContext(options);
        var retry = await new TaskService(retryContext).CompleteAsync(original.Id,
            new CompleteTaskRequestDto { CompletionNotes = "Retry notes", CompletedBy = "Retry keeper" }, token);
        Assert.Equal(results[0].Task!.CompletedAt, retry.Task!.CompletedAt);
        Assert.Equal("First keeper", retry.Task.CompletedBy);
        await AssertCompletedAsync(options, original, recurrence, "First notes");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailureOrCancellationAfterSave_RollsBackAndAllowsRetry(bool cancel)
    {
        var options = await database.CreateDatabaseAsync();
        var original = await SeedAsync(options, RecurrenceType.Daily);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        if (!cancel)
        {
            // A deferred constraint fails at commit, after all task/activity writes succeeded.
            await using var setup = new ProfessorDbContext(options);
            await setup.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION reject_test_completion() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Test completion commit failure'; END;
                $$;
                CREATE CONSTRAINT TRIGGER reject_test_completion
                AFTER UPDATE ON "Tasks" DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW WHEN (NEW."IsCompleted") EXECUTE FUNCTION reject_test_completion();
                """);
        }
        var gate = new BeforeCommitGate();
        await using (var context = new ProfessorDbContext(new DbContextOptionsBuilder<ProfessorDbContext>(options)
            .AddInterceptors(gate).Options))
        {
            var completion = new TaskService(context).CompleteAsync(original.Id,
                new CompleteTaskRequestDto { CompletionNotes = "Must roll back" }, cancellation.Token);
            await gate.Saved.Task.WaitAsync(cancellation.Token);
            if (cancel)
            {
                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completion);
            }
            else
            {
                gate.Release.TrySetResult();
                await Assert.ThrowsAsync<PostgresException>(() => completion);
            }
        }
        await using (var verification = new ProfessorDbContext(options))
        {
            var persisted = await verification.Tasks.SingleAsync();
            Assert.False(persisted.IsCompleted);
            Assert.Null(persisted.CompletedAt);
            Assert.Null(persisted.CompletionNotes);
            Assert.Null(persisted.CompletedBy);
            Assert.Empty(await verification.ActivityEvents.ToListAsync());
            Assert.Empty(await verification.ActivityEventAnimals.ToListAsync());
            Assert.Empty(await verification.ActivityEventEnclosures.ToListAsync());
        }
        await using var retryContext = new ProfessorDbContext(options);
        if (!cancel)
        {
            await retryContext.Database.ExecuteSqlRawAsync("""
                DROP TRIGGER reject_test_completion ON "Tasks";
                DROP FUNCTION reject_test_completion();
                """);
        }
        var retry = await new TaskService(retryContext).CompleteAsync(original.Id,
            new CompleteTaskRequestDto { CompletionNotes = "Retry succeeds" });
        Assert.Equal(CompleteTaskResultStatus.Completed, retry.Status);
        await AssertCompletedAsync(options, original, RecurrenceType.Daily, "Retry succeeds");
    }

    [Fact]
    public async Task MissingTask_ReturnsNotFound()
    {
        var options = await database.CreateDatabaseAsync();
        await using var context = new ProfessorDbContext(options);
        var result = await new TaskService(context).CompleteAsync(int.MaxValue, new CompleteTaskRequestDto());
        Assert.Equal(CompleteTaskResultStatus.NotFound, result.Status);
    }

    private static async Task<TaskEntity> SeedAsync(DbContextOptions<ProfessorDbContext> options, RecurrenceType recurrence)
    {
        await using var context = new ProfessorDbContext(options);
        var now = DateTime.UtcNow;
        var enclosure = new skipper_api.Domain.Enclosures.Enclosure
        {
            Name = "Task test enclosure", Type = skipper_api.Domain.Enclosures.EnclosureType.Other,
            Location = "Test", Mobility = skipper_api.Domain.Enclosures.EnclosureMobility.Fixed,
            Status = skipper_api.Domain.Enclosures.EnclosureStatus.Active, CreatedDate = now, UpdatedDate = now,
        };
        var animal = new skipper_api.Domain.Animals.Animal
        {
            Name = "Task test animal", Species = "Ferret", AnimalType = skipper_api.Domain.Animals.AnimalType.Mammal,
            Status = skipper_api.Domain.Animals.AnimalStatus.Active, Sex = skipper_api.Domain.Animals.AnimalSex.Male,
            BirthDateIsEstimated = false, AcquiredDate = DateOnly.FromDateTime(now),
            Enclosure = enclosure, CreatedAt = now, UpdatedAt = now,
        };
        var task = new TaskEntity
        {
            Title = "Inspection", Description = "Inspect habitat", TaskType = TaskType.Inspection,
            DueAt = now, RecurrenceType = recurrence, RecurrenceInterval = recurrence == RecurrenceType.None ? null : 2,
            IsCompleted = false, Animal = animal, Enclosure = enclosure, CreatedAt = now, UpdatedAt = now,
        };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        return task;
    }

    private static async Task AssertCompletedAsync(DbContextOptions<ProfessorDbContext> options,
        TaskEntity original, RecurrenceType recurrence, string notes)
    {
        await using var context = new ProfessorDbContext(options);
        var tasks = await context.Tasks.OrderBy(task => task.Id).ToListAsync();
        var completed = Assert.Single(tasks, task => task.IsCompleted);
        Assert.Equal(notes, completed.CompletionNotes);
        var activity = Assert.Single(await context.ActivityEvents.Include(item => item.Animals)
            .Include(item => item.Enclosures).ToListAsync());
        Assert.Equal(completed.CompletedAt, activity.OccurredAt);
        Assert.Equal(notes, activity.Notes);
        Assert.Equal(original.AnimalId, Assert.Single(activity.Animals).AnimalId);
        Assert.Equal(original.EnclosureId, Assert.Single(activity.Enclosures).EnclosureId);
        Assert.Equal(original.Id, activity.Metadata!.RootElement.GetProperty("taskId").GetInt32());
        if (recurrence == RecurrenceType.None)
        {
            Assert.Single(tasks);
            return;
        }
        Assert.Equal(2, tasks.Count);
        var successor = Assert.Single(tasks, task => !task.IsCompleted);
        Assert.Equal(original.Title, successor.Title);
        Assert.Equal(original.Description, successor.Description);
        Assert.Equal(original.TaskType, successor.TaskType);
        Assert.Equal(original.DueAt.AddDays(2), successor.DueAt, TimeSpan.FromMilliseconds(1));
        Assert.Equal(original.RecurrenceType, successor.RecurrenceType);
        Assert.Equal(original.RecurrenceInterval, successor.RecurrenceInterval);
        Assert.Equal(original.AnimalId, successor.AnimalId);
        Assert.Equal(original.EnclosureId, successor.EnclosureId);
        Assert.Null(successor.CompletedAt);
        Assert.Null(successor.CompletionNotes);
        Assert.Null(successor.CompletedBy);
    }

    private static async Task WaitForLockAsync(DbContextOptions<ProfessorDbContext> options, int pid, CancellationToken token)
    {
        await using var observer = new ProfessorDbContext(options);
        while (await observer.Database.SqlQuery<int>($"""
            SELECT count(*)::integer AS "Value" FROM pg_stat_activity
            WHERE pid = {pid} AND wait_event_type = 'Lock'
            """).SingleAsync(token) != 1)
        {
            await Task.Delay(20, token);
        }
    }

    private sealed class BeforeCommitGate : SaveChangesInterceptor
    {
        public TaskCompletionSource Saved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData,
            int result, CancellationToken cancellationToken = default)
        {
            Saved.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return result;
        }
    }
}
