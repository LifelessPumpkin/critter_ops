using Skipper.Api.Tests.Data;
using Microsoft.EntityFrameworkCore;
using skipper_api.Data;
using skipper_api.Domain.Activity;
using skipper_api.Domain.Animals;
using skipper_api.Domain.Enclosures;
using skipper_api.Dtos.Tasks;
using skipper_api.Services.Tasks;
using Xunit;
using RecurrenceType = skipper_api.Domain.Tasks.RecurrenceType;
using TaskEntity = skipper_api.Domain.Tasks.Task;
using TaskType = skipper_api.Domain.Tasks.TaskType;

namespace Skipper.Api.Tests;

public sealed class TaskActivityIntegrationTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _database;
    public TaskActivityIntegrationTests(PostgresFixture database) => _database = database;

    [Fact]
    public async Task CompleteFeedingTask_PersistsCompletionDetailsAndRelationships()
    {
        await using var fixture = await TestFixture.CreateAsync(_database);
        var task = fixture.AddTask(TaskType.Feeding, fixture.OtherEnclosure.Id);
        await fixture.SaveAndDetachAsync();
        var beforeCompletion = DateTime.UtcNow;

        var result = await fixture.Tasks.CompleteAsync(task.Id, new CompleteTaskRequestDto
        {
            CompletionNotes = "Food and water refreshed.",
            CompletedBy = "Logan",
        });

        var completedTask = await fixture.Context.Tasks.AsNoTracking().SingleAsync(item => item.Id == task.Id);
        var activity = await fixture.ActivityWithDetails().SingleAsync();

        Assert.Equal(CompleteTaskResultStatus.Completed, result.Status);
        Assert.True(completedTask.IsCompleted);
        Assert.InRange(completedTask.CompletedAt!.Value, beforeCompletion, DateTime.UtcNow);
        Assert.Equal(completedTask.CompletedAt, activity.OccurredAt);
        Assert.Equal(ActivityEventType.Feeding, activity.EventType);
        Assert.Equal(task.Title, activity.Title);
        Assert.Equal("Food and water refreshed.", activity.Notes);
        Assert.Equal("Logan", activity.PerformedBy);
        Assert.Equal(task.AnimalId, Assert.Single(activity.Animals).AnimalId);
        Assert.Equal(fixture.OtherEnclosure.Id, Assert.Single(activity.Enclosures).EnclosureId);
        Assert.NotNull(activity.AnimalFeeding);
        Assert.Equal(task.Id, activity.Metadata!.RootElement.GetProperty("taskId").GetInt32());
        Assert.Equal(TaskType.Feeding.ToString(), activity.Metadata.RootElement.GetProperty("taskType").GetString());
    }

    [Fact]
    public async Task CompleteOtherTask_PreservesGenericTaskActivityAndIdempotency()
    {
        await using var fixture = await TestFixture.CreateAsync(_database);
        var task = fixture.AddTask(TaskType.Other, fixture.Animal.EnclosureId);
        await fixture.SaveAndDetachAsync();

        var firstResult = await fixture.Tasks.CompleteAsync(task.Id, new CompleteTaskRequestDto());
        var repeatedResult = await fixture.Tasks.CompleteAsync(task.Id, new CompleteTaskRequestDto());
        var activity = await fixture.Context.ActivityEvents.AsNoTracking().SingleAsync();

        Assert.Equal(CompleteTaskResultStatus.Completed, firstResult.Status);
        Assert.Equal(CompleteTaskResultStatus.AlreadyCompleted, repeatedResult.Status);
        Assert.Equal(ActivityEventType.Task, activity.EventType);
        Assert.Equal($"Task completed: {task.Title}", activity.Title);
        Assert.Equal(1, await fixture.Context.ActivityEvents.CountAsync());
    }

    [Fact]
    public async Task CompleteEnclosureOnlyTask_PreservesOnlyEnclosureRelationship()
    {
        await using var fixture = await TestFixture.CreateAsync(_database);
        var task = fixture.AddTask(
            TaskType.Cleaning,
            fixture.OtherEnclosure.Id,
            includeAnimal: false);
        await fixture.SaveAndDetachAsync();

        await fixture.Tasks.CompleteAsync(task.Id, new CompleteTaskRequestDto());

        var activity = await fixture.ActivityWithDetails().SingleAsync();

        Assert.Equal(ActivityEventType.Cleaning, activity.EventType);
        Assert.NotNull(activity.EnclosureCleaning);
        Assert.Empty(activity.Animals);
        Assert.Equal(fixture.OtherEnclosure.Id, Assert.Single(activity.Enclosures).EnclosureId);
    }

    [Fact]
    public async Task CompleteRecurringGeneralTask_CreatesOnlyOneActivityAndOneFutureTask()
    {
        await using var fixture = await TestFixture.CreateAsync(_database);
        var task = fixture.AddTask(TaskType.General, fixture.Animal.EnclosureId, RecurrenceType.Daily);
        await fixture.SaveAndDetachAsync();

        await fixture.Tasks.CompleteAsync(task.Id, new CompleteTaskRequestDto());

        Assert.Equal(1, await fixture.Context.ActivityEvents.CountAsync());
        var tasks = await fixture.Context.Tasks.AsNoTracking().OrderBy(item => item.Id).ToListAsync();
        Assert.Equal(2, tasks.Count);
        Assert.True(tasks[0].IsCompleted);
        Assert.False(tasks[1].IsCompleted);
        Assert.Equal(TaskType.General, tasks[1].TaskType);
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private TestFixture(ProfessorDbContext context)
        {
            Context = context;
            Tasks = new TaskService(context);
        }

        public ProfessorDbContext Context { get; }

        public TaskService Tasks { get; }

        public Animal Animal { get; private set; } = null!;

        public Enclosure OtherEnclosure { get; private set; } = null!;

        public static async Task<TestFixture> CreateAsync(PostgresFixture database)
        {
            var options = await database.CreateDatabaseAsync();
            var context = new ProfessorDbContext(options);
            var fixture = new TestFixture(context);
            await fixture.SeedAsync();
            return fixture;
        }

        public TaskEntity AddTask(
            TaskType taskType,
            int? enclosureId,
            RecurrenceType recurrenceType = RecurrenceType.None,
            bool includeAnimal = true)
        {
            var now = DateTime.UtcNow;
            var task = new TaskEntity
            {
                Title = $"Gonzo {taskType}",
                Description = $"Routine {taskType} work",
                TaskType = taskType,
                DueAt = now,
                RecurrenceType = recurrenceType,
                RecurrenceInterval = recurrenceType == RecurrenceType.None ? null : 1,
                IsCompleted = false,
                AnimalId = includeAnimal ? Animal.Id : null,
                EnclosureId = enclosureId,
                CreatedAt = now,
                UpdatedAt = now,
            };
            Context.Tasks.Add(task);
            return task;
        }

        public IQueryable<ActivityEvent> ActivityWithDetails()
        {
            return Context.ActivityEvents
                .AsNoTracking()
                .Include(item => item.Animals)
                .Include(item => item.Enclosures)
                .Include(item => item.AnimalFeeding)
                .Include(item => item.AnimalMedication)
                .Include(item => item.EnclosureCleaning);
        }

        public async Task SaveAndDetachAsync()
        {
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
        }

        private async Task SeedAsync()
        {
            var now = DateTime.UtcNow;
            var currentEnclosure = CreateEnclosure("Ferret Room", now);
            OtherEnclosure = CreateEnclosure("Feeding Station", now);
            Context.Enclosures.AddRange(currentEnclosure, OtherEnclosure);
            Animal = new Animal
            {
                Name = "Gonzo",
                Species = "Ferret",
                AnimalType = AnimalType.Mammal,
                Status = AnimalStatus.Active,
                Sex = AnimalSex.Male,
                BirthDateIsEstimated = false,
                AcquiredDate = DateOnly.FromDateTime(now),
                Enclosure = currentEnclosure,
                CreatedAt = now,
                UpdatedAt = now,
            };
            Context.Animals.Add(Animal);
            await Context.SaveChangesAsync();
        }

        private static Enclosure CreateEnclosure(string name, DateTime now)
        {
            return new Enclosure
            {
                Name = name,
                Type = EnclosureType.Other,
                Location = name,
                Mobility = EnclosureMobility.Fixed,
                Status = EnclosureStatus.Active,
                CreatedDate = now,
                UpdatedDate = now,
            };
        }
    }

}
