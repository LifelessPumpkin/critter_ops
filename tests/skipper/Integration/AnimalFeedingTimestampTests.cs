using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Skipper.Api.Tests.Data;
using skipper_api.Data;
using skipper_api.Domain.Animals;
using skipper_api.Domain.Enclosures;
using skipper_api.Dtos.AnimalFeedings;
using skipper_api.Services.AnimalFeedings;
using Xunit;

namespace Skipper.Api.Tests;

public sealed class AnimalFeedingTimestampTests(PostgresFixture database) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData("2026-11-19T12:00:00Z")]
    [InlineData("2026-11-19T12:00:00+00:00")]
    [InlineData("2026-11-19T07:00:00-05:00")]
    public async Task CreateAndUpdate_PersistOffsetTimestampsAsUtc(string timestamp)
    {
        await using var context = new ProfessorDbContext(await database.CreateDatabaseAsync());
        var now = DateTime.UtcNow;
        var animal = new Animal
        {
            Name = "Timestamp regression",
            Species = "Ferret",
            AnimalType = AnimalType.Mammal,
            Status = AnimalStatus.Active,
            Sex = AnimalSex.Unknown,
            BirthDateIsEstimated = false,
            AcquiredDate = DateOnly.FromDateTime(now),
            CreatedAt = now,
            UpdatedAt = now,
            Enclosure = new Enclosure
            {
                Name = "Test habitat", Location = "Test room", Type = EnclosureType.Other,
                Mobility = EnclosureMobility.Fixed, Status = EnclosureStatus.Active,
                CreatedDate = now, UpdatedDate = now,
            },
        };
        context.Animals.Add(animal);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Deserialize the actual request contracts: explicit offsets become local-kind DateTimes.
        var json = $$"""{"Food":"Meal","Quantity":1,"Unit":0,"Result":0,"OccurredAt":"{{timestamp}}"}""";
        var service = new AnimalFeedingActivityService(context);
        var created = await service.CreateAsync(animal.Id, JsonSerializer.Deserialize<CreateAnimalFeedingActivityDto>(json)!);
        Assert.Equal(CreateAnimalFeedingActivityResultStatus.Created, created.Status);
        var activityId = Assert.IsType<AnimalFeedingActivityDto>(created.Feeding).ActivityEventId;
        context.ChangeTracker.Clear();
        var expected = new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);
        var stored = await context.ActivityEvents.AsNoTracking().SingleAsync(item => item.Id == activityId);
        Assert.Equal(expected, stored.OccurredAt);
        Assert.Equal(DateTimeKind.Utc, stored.OccurredAt.Kind);

        var updated = await service.UpdateAsync(animal.Id, activityId, JsonSerializer.Deserialize<UpdateAnimalFeedingActivityDto>(json)!);
        Assert.Equal(UpdateAnimalFeedingActivityResultStatus.Updated, updated.Status);
        context.ChangeTracker.Clear();
        stored = await context.ActivityEvents.AsNoTracking().SingleAsync(item => item.Id == activityId);
        Assert.Equal(expected, stored.OccurredAt);
        Assert.Equal(DateTimeKind.Utc, stored.OccurredAt.Kind);
    }
}
