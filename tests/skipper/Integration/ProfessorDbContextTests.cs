using Microsoft.EntityFrameworkCore;
using skipper_api.Data;
using skipper_api.Domain.Enclosures;
using Xunit;

namespace Skipper.Api.Tests.Data;

public class ProfessorDbContextTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _database;
    public ProfessorDbContextTests(PostgresFixture database) => _database = database;

    [Fact]
    public async Task ProfessorDbContext_CanPersistAndRetrieveEnclosure()
    {
        var options = await _database.CreateDatabaseAsync();

        int enclosureId;
        var createdAt = DateTime.UtcNow;

        await using (var setupContext = new ProfessorDbContext(options))
        {

            var enclosure = new Enclosure
            {
                Name = "Reptile Room Terrarium 1",
                Type = EnclosureType.Terrarium,
                Location = "Reptile Room",
                Mobility = EnclosureMobility.Movable,
                Status = EnclosureStatus.Active,
                CreatedDate = createdAt,
                UpdatedDate = createdAt,
            };

            setupContext.Enclosures.Add(enclosure);
            await setupContext.SaveChangesAsync();

            enclosureId = enclosure.Id;
        }

        await using var assertionContext = new ProfessorDbContext(options);
        var persistedEnclosure = await assertionContext.Enclosures
            .SingleAsync(enclosure => enclosure.Id == enclosureId);

        Assert.True(persistedEnclosure.Id > 0);
        Assert.Equal("Reptile Room Terrarium 1", persistedEnclosure.Name);
        Assert.Equal(EnclosureType.Terrarium, persistedEnclosure.Type);
        Assert.Equal("Reptile Room", persistedEnclosure.Location);
        Assert.Equal(EnclosureStatus.Active, persistedEnclosure.Status);
    }
    [Fact]
    public async Task CompleteRecurringTask_IsIdempotentAndPersistsOneActivity()
    {
        var options = await _database.CreateDatabaseAsync();
        await using var context = new ProfessorDbContext(options);
        var dueAt = DateTime.UtcNow;
        var task = new skipper_api.Domain.Tasks.Task
        {
            Title = "Daily inspection", TaskType = skipper_api.Domain.Tasks.TaskType.General,
            DueAt = dueAt, RecurrenceType = skipper_api.Domain.Tasks.RecurrenceType.Daily,
            IsCompleted = false, CreatedAt = dueAt, UpdatedAt = dueAt,
        };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        var service = new skipper_api.Services.Tasks.TaskService(context);
        var result = await service.CompleteAsync(task.Id, new skipper_api.Dtos.Tasks.CompleteTaskRequestDto());
        var repeated = await service.CompleteAsync(task.Id, new skipper_api.Dtos.Tasks.CompleteTaskRequestDto());
        Assert.Equal(skipper_api.Services.Tasks.CompleteTaskResultStatus.Completed, result.Status);
        Assert.Equal(skipper_api.Services.Tasks.CompleteTaskResultStatus.AlreadyCompleted, repeated.Status);
        context.ChangeTracker.Clear();
        Assert.True((await context.Tasks.SingleAsync(item => item.Id == task.Id)).IsCompleted);
        var next = await context.Tasks.SingleAsync(item => !item.IsCompleted);
        Assert.Equal(dueAt.AddDays(1), next.DueAt, TimeSpan.FromMilliseconds(1));
        Assert.Single(await context.ActivityEvents.ToListAsync());
    }

}
