using Microsoft.EntityFrameworkCore;
using skipper_api.Data;
using skipper_api.Domain.Activity;
using skipper_api.Dtos.AnimalFeedings;

namespace skipper_api.Services.AnimalFeedings;

public class AnimalFeedingActivityService : IAnimalFeedingActivityService
{
    private readonly ProfessorDbContext _dbContext;

    public AnimalFeedingActivityService(ProfessorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AnimalFeedingActivityDto>?> GetByAnimalIdAsync(
        int animalId,
        CancellationToken cancellationToken = default)
    {
        var animalExists = await _dbContext.Animals
            .AsNoTracking()
            .AnyAsync(animal => animal.Id == animalId, cancellationToken);

        if (!animalExists)
        {
            return null;
        }

        var feedingEvents = await FeedingEvents()
            .Where(activityEvent => activityEvent.Animals.Any(association => association.AnimalId == animalId))
            .OrderByDescending(activityEvent => activityEvent.OccurredAt)
            .ThenByDescending(activityEvent => activityEvent.Id)
            .ToListAsync(cancellationToken);

        return feedingEvents.Select(activityEvent => ToDto(activityEvent, animalId)).ToList();
    }

    public async Task<AnimalFeedingActivityDto?> GetByIdAsync(
        int animalId,
        long activityId,
        CancellationToken cancellationToken = default)
    {
        var feedingEvent = await FeedingEvents()
            .Where(activityEvent =>
                activityEvent.Id == activityId &&
                activityEvent.Animals.Any(association => association.AnimalId == animalId))
            .SingleOrDefaultAsync(cancellationToken);

        return feedingEvent is null
            ? null
            : ToDto(feedingEvent, animalId);
    }

    public async Task<CreateAnimalFeedingActivityResult> CreateAsync(
        int animalId,
        CreateAnimalFeedingActivityDto request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var animal = await _dbContext.Animals
            .Include(animal => animal.Enclosure)
            .SingleOrDefaultAsync(animal => animal.Id == animalId, cancellationToken);

        if (animal is null)
        {
            return CreateAnimalFeedingActivityResult.AnimalNotFound();
        }

        if (animal.Enclosure is null)
        {
            return CreateAnimalFeedingActivityResult.EnclosureNotFound();
        }

        var enclosureId = animal.EnclosureId;
        var enclosureName = animal.Enclosure.Name;
        var now = DateTime.UtcNow;
        var activityEvent = new ActivityEvent
        {
            EventType = ActivityEventType.Feeding,
            OccurredAt = request.OccurredAt!.Value,
            Title = ToAnimalTimelineTitle(request.Quantity!.Value, request.Unit!.Value, request.Food!, request.Result!.Value),
            Notes = request.Notes,
            PerformedBy = request.PerformedBy,
            CreatedAt = now,
            UpdatedAt = now,
            Animals =
            [
                new ActivityEventAnimal
                {
                    AnimalId = animalId,
                    RelationshipType = ActivityEventAnimalRelationshipType.Primary,
                },
            ],
            Enclosures =
            [
                new ActivityEventEnclosure
                {
                    EnclosureId = enclosureId,
                    RelationshipType = ActivityEventEnclosureRelationshipType.Primary,
                },
            ],
            AnimalFeeding = new AnimalFeedingActivity
            {
                Food = request.Food!,
                Quantity = request.Quantity.Value,
                Unit = request.Unit.Value,
                Result = request.Result.Value,
            },
        };

        _dbContext.ActivityEvents.Add(activityEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return CreateAnimalFeedingActivityResult.Created(ToDto(
            activityEvent,
            animalId,
            animal.Name,
            enclosureId,
            enclosureName));
    }

    public async Task<UpdateAnimalFeedingActivityResult> UpdateAsync(
        int animalId,
        long activityId,
        UpdateAnimalFeedingActivityDto request,
        CancellationToken cancellationToken = default)
    {
        var feedingEvent = await _dbContext.ActivityEvents
            .Include(activityEvent => activityEvent.AnimalFeeding)
            .Include(activityEvent => activityEvent.Animals)
                .ThenInclude(association => association.Animal)
            .Include(activityEvent => activityEvent.Enclosures)
                .ThenInclude(association => association.Enclosure)
            .SingleOrDefaultAsync(
                activityEvent =>
                    activityEvent.Id == activityId &&
                    activityEvent.EventType == ActivityEventType.Feeding &&
                    activityEvent.Animals.Any(association => association.AnimalId == animalId),
                cancellationToken);

        if (feedingEvent?.AnimalFeeding is null)
        {
            return UpdateAnimalFeedingActivityResult.NotFound();
        }

        feedingEvent.OccurredAt = request.OccurredAt!.Value;
        feedingEvent.Notes = request.Notes;
        feedingEvent.PerformedBy = request.PerformedBy;
        feedingEvent.Title = ToAnimalTimelineTitle(
            request.Quantity!.Value,
            request.Unit!.Value,
            request.Food!,
            request.Result!.Value);
        feedingEvent.UpdatedAt = DateTime.UtcNow;
        feedingEvent.AnimalFeeding.Food = request.Food!;
        feedingEvent.AnimalFeeding.Quantity = request.Quantity.Value;
        feedingEvent.AnimalFeeding.Unit = request.Unit.Value;
        feedingEvent.AnimalFeeding.Result = request.Result.Value;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return UpdateAnimalFeedingActivityResult.Updated(ToDto(feedingEvent, animalId));
    }

    public async Task<DeleteAnimalFeedingActivityResult> DeleteAsync(
        int animalId,
        long activityId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var feedingEvent = await _dbContext.ActivityEvents
            .Include(activityEvent => activityEvent.AnimalFeeding)
            .Include(activityEvent => activityEvent.Animals)
            .SingleOrDefaultAsync(
                activityEvent =>
                    activityEvent.Id == activityId &&
                    activityEvent.EventType == ActivityEventType.Feeding &&
                    activityEvent.Animals.Any(association => association.AnimalId == animalId),
                cancellationToken);

        if (feedingEvent?.AnimalFeeding is null)
        {
            return DeleteAnimalFeedingActivityResult.NotFound;
        }

        _dbContext.ActivityEvents.Remove(feedingEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return DeleteAnimalFeedingActivityResult.Deleted;
    }

    private IQueryable<ActivityEvent> FeedingEvents()
    {
        return _dbContext.ActivityEvents
            .AsNoTracking()
            .Include(activityEvent => activityEvent.AnimalFeeding)
            .Include(activityEvent => activityEvent.Animals)
                .ThenInclude(association => association.Animal)
            .Include(activityEvent => activityEvent.Enclosures)
                .ThenInclude(association => association.Enclosure)
            .Where(activityEvent => activityEvent.EventType == ActivityEventType.Feeding &&
                activityEvent.AnimalFeeding != null);
    }

    private static AnimalFeedingActivityDto ToDto(ActivityEvent activityEvent, int animalId)
    {
        var animalAssociation = activityEvent.Animals.Single(association => association.AnimalId == animalId);
        var enclosureAssociation = activityEvent.Enclosures
            .OrderBy(association => association.RelationshipType == ActivityEventEnclosureRelationshipType.Primary ? 0 : 1)
            .First();

        return ToDto(
            activityEvent,
            animalId,
            animalAssociation.Animal.Name,
            enclosureAssociation.EnclosureId,
            enclosureAssociation.Enclosure.Name);
    }

    private static AnimalFeedingActivityDto ToDto(
        ActivityEvent activityEvent,
        int animalId,
        string animalName,
        int enclosureId,
        string enclosureName)
    {
        var feeding = activityEvent.AnimalFeeding!;

        return new AnimalFeedingActivityDto
        {
            ActivityEventId = activityEvent.Id,
            AnimalId = animalId,
            AnimalName = animalName,
            EnclosureId = enclosureId,
            EnclosureName = enclosureName,
            Food = feeding.Food,
            Quantity = feeding.Quantity,
            Unit = feeding.Unit,
            Result = feeding.Result,
            OccurredAt = activityEvent.OccurredAt,
            Notes = activityEvent.Notes,
            PerformedBy = activityEvent.PerformedBy,
            CreatedAt = activityEvent.CreatedAt,
            UpdatedAt = activityEvent.UpdatedAt,
        };
    }

    private static string ToAnimalTimelineTitle(
        decimal quantity,
        AnimalFeedingQuantityUnit unit,
        string food,
        AnimalFeedingResult result)
    {
        return $"Fed {FormatFeedingAmount(quantity, unit, food)} - {result}";
    }

    public static string FormatFeedingAmount(decimal quantity, AnimalFeedingQuantityUnit unit, string food)
    {
        var formattedQuantity = quantity % 1 == 0
            ? decimal.Truncate(quantity).ToString("0")
            : quantity.ToString("0.####");

        return unit == AnimalFeedingQuantityUnit.Item
            ? $"{formattedQuantity} {food}"
            : $"{formattedQuantity} {FormatUnit(unit, quantity)} {food}";
    }

    private static string FormatUnit(AnimalFeedingQuantityUnit unit, decimal quantity)
    {
        var label = unit switch
        {
            AnimalFeedingQuantityUnit.Gram => "gram",
            AnimalFeedingQuantityUnit.Kilogram => "kilogram",
            AnimalFeedingQuantityUnit.Ounce => "ounce",
            AnimalFeedingQuantityUnit.Pound => "pound",
            AnimalFeedingQuantityUnit.Milliliter => "milliliter",
            AnimalFeedingQuantityUnit.Liter => "liter",
            AnimalFeedingQuantityUnit.Teaspoon => "teaspoon",
            AnimalFeedingQuantityUnit.Tablespoon => "tablespoon",
            AnimalFeedingQuantityUnit.Cup => "cup",
            AnimalFeedingQuantityUnit.Other => "unit",
            _ => "item",
        };

        return quantity == 1 ? label : $"{label}s";
    }
}
