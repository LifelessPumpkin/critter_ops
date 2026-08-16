using Microsoft.EntityFrameworkCore;
using skipper_api.Data;
using skipper_api.Domain.Activity;
using skipper_api.Dtos.AnimalMovements;

namespace skipper_api.Services.AnimalMovements;

public class AnimalMovementActivityService : IAnimalMovementActivityService
{
    private readonly ProfessorDbContext _dbContext;

    public AnimalMovementActivityService(ProfessorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AnimalMovementActivityDto>?> GetByAnimalIdAsync(
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

        var movementEvents = await MovementEvents()
            .Where(activityEvent => activityEvent.Animals.Any(association => association.AnimalId == animalId))
            .OrderByDescending(activityEvent => activityEvent.OccurredAt)
            .ThenByDescending(activityEvent => activityEvent.Id)
            .ToListAsync(cancellationToken);

        return movementEvents.Select(activityEvent => ToDto(activityEvent, animalId)).ToList();
    }

    public async Task<AnimalMovementActivityDto?> GetByIdAsync(
        int animalId,
        long activityId,
        CancellationToken cancellationToken = default)
    {
        var movementEvent = await MovementEvents()
            .Where(activityEvent =>
                activityEvent.Id == activityId &&
                activityEvent.Animals.Any(association => association.AnimalId == animalId))
            .SingleOrDefaultAsync(cancellationToken);

        return movementEvent is null
            ? null
            : ToDto(movementEvent, animalId);
    }

    public async Task<CreateAnimalMovementActivityResult> CreateAsync(
        int animalId,
        CreateAnimalMovementActivityDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.FromEnclosureId is not { } fromEnclosureId)
        {
            return CreateAnimalMovementActivityResult.SourceEnclosureNotFound();
        }

        if (request.ToEnclosureId is not { } toEnclosureId)
        {
            return CreateAnimalMovementActivityResult.DestinationEnclosureNotFound();
        }

        if (fromEnclosureId == toEnclosureId)
        {
            return CreateAnimalMovementActivityResult.DestinationMatchesCurrentEnclosure();
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var animal = await LockAnimalAsync(animalId, cancellationToken);
        if (animal is null)
        {
            return CreateAnimalMovementActivityResult.AnimalNotFound();
        }

        var fromEnclosureName = await GetEnclosureNameAsync(fromEnclosureId, cancellationToken);
        if (fromEnclosureName is null)
        {
            return CreateAnimalMovementActivityResult.SourceEnclosureNotFound();
        }

        var toEnclosureName = await GetEnclosureNameAsync(toEnclosureId, cancellationToken);
        if (toEnclosureName is null)
        {
            return CreateAnimalMovementActivityResult.DestinationEnclosureNotFound();
        }

        if (animal.EnclosureId != fromEnclosureId)
        {
            return CreateAnimalMovementActivityResult.SourceDoesNotMatchCurrentEnclosure();
        }

        if (animal.EnclosureId == toEnclosureId)
        {
            return CreateAnimalMovementActivityResult.DestinationMatchesCurrentEnclosure();
        }

        var now = DateTime.UtcNow;
        var activityEvent = new ActivityEvent
        {
            EventType = ActivityEventType.AnimalMovement,
            OccurredAt = request.OccurredAt!.Value,
            Title = $"Moved from {fromEnclosureName} to {toEnclosureName}",
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
                    EnclosureId = fromEnclosureId,
                    RelationshipType = ActivityEventEnclosureRelationshipType.Source,
                },
                new ActivityEventEnclosure
                {
                    EnclosureId = toEnclosureId,
                    RelationshipType = ActivityEventEnclosureRelationshipType.Destination,
                },
            ],
            AnimalMovement = new AnimalMovementActivity
            {
                FromEnclosureId = fromEnclosureId,
                ToEnclosureId = toEnclosureId,
                Reason = request.Reason,
            },
        };

        animal.EnclosureId = toEnclosureId;
        animal.UpdatedAt = now;

        _dbContext.ActivityEvents.Add(activityEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return CreateAnimalMovementActivityResult.Created(ToDto(
            activityEvent,
            animalId,
            animal.Name,
            fromEnclosureName,
            toEnclosureName));
    }

    public async Task<UpdateAnimalMovementActivityResult> UpdateAsync(
        int animalId,
        long activityId,
        UpdateAnimalMovementActivityDto request,
        CancellationToken cancellationToken = default)
    {
        var movementEvent = await _dbContext.ActivityEvents
            .Include(activityEvent => activityEvent.AnimalMovement)
                .ThenInclude(movement => movement!.FromEnclosure)
            .Include(activityEvent => activityEvent.AnimalMovement)
                .ThenInclude(movement => movement!.ToEnclosure)
            .Include(activityEvent => activityEvent.Animals)
                .ThenInclude(association => association.Animal)
            .SingleOrDefaultAsync(
                activityEvent =>
                    activityEvent.Id == activityId &&
                    activityEvent.EventType == ActivityEventType.AnimalMovement &&
                    activityEvent.Animals.Any(association => association.AnimalId == animalId),
                cancellationToken);

        if (movementEvent?.AnimalMovement is null)
        {
            return UpdateAnimalMovementActivityResult.NotFound();
        }

        movementEvent.OccurredAt = request.OccurredAt!.Value;
        movementEvent.Notes = request.Notes;
        movementEvent.PerformedBy = request.PerformedBy;
        movementEvent.UpdatedAt = DateTime.UtcNow;
        movementEvent.AnimalMovement.Reason = request.Reason;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return UpdateAnimalMovementActivityResult.Updated(ToDto(movementEvent, animalId));
    }

    public async Task<DeleteAnimalMovementActivityResult> DeleteAsync(
        int animalId,
        long activityId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var animal = await LockAnimalAsync(animalId, cancellationToken);
        if (animal is null)
        {
            return DeleteAnimalMovementActivityResult.NotFound;
        }

        var movementEvent = await _dbContext.ActivityEvents
            .Include(activityEvent => activityEvent.AnimalMovement)
            .Include(activityEvent => activityEvent.Animals)
            .SingleOrDefaultAsync(
                activityEvent =>
                    activityEvent.Id == activityId &&
                    activityEvent.EventType == ActivityEventType.AnimalMovement &&
                    activityEvent.Animals.Any(association => association.AnimalId == animalId),
                cancellationToken);

        if (movementEvent?.AnimalMovement is null)
        {
            return DeleteAnimalMovementActivityResult.NotFound;
        }

        var laterMovementExists = await _dbContext.ActivityEvents
            .AsNoTracking()
            .AnyAsync(
                activityEvent =>
                    activityEvent.EventType == ActivityEventType.AnimalMovement &&
                    activityEvent.Animals.Any(association => association.AnimalId == animalId) &&
                    (activityEvent.OccurredAt > movementEvent.OccurredAt ||
                        (activityEvent.OccurredAt == movementEvent.OccurredAt && activityEvent.Id > movementEvent.Id)),
                cancellationToken);

        if (laterMovementExists)
        {
            return DeleteAnimalMovementActivityResult.NotLatestMovement;
        }

        if (animal.EnclosureId != movementEvent.AnimalMovement.ToEnclosureId)
        {
            return DeleteAnimalMovementActivityResult.AnimalLocationMismatch;
        }

        animal.EnclosureId = movementEvent.AnimalMovement.FromEnclosureId;
        animal.UpdatedAt = DateTime.UtcNow;

        _dbContext.ActivityEvents.Remove(movementEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return DeleteAnimalMovementActivityResult.Deleted;
    }

    private IQueryable<ActivityEvent> MovementEvents()
    {
        return _dbContext.ActivityEvents
            .AsNoTracking()
            .Include(activityEvent => activityEvent.AnimalMovement)
                .ThenInclude(movement => movement!.FromEnclosure)
            .Include(activityEvent => activityEvent.AnimalMovement)
                .ThenInclude(movement => movement!.ToEnclosure)
            .Include(activityEvent => activityEvent.Animals)
                .ThenInclude(association => association.Animal)
            .Where(activityEvent => activityEvent.EventType == ActivityEventType.AnimalMovement);
    }

    private async Task<Domain.Animals.Animal?> LockAnimalAsync(
        int animalId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Animals
            .FromSqlInterpolated($"SELECT * FROM \"Animals\" WHERE \"Id\" = {animalId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<string?> GetEnclosureNameAsync(int enclosureId, CancellationToken cancellationToken)
    {
        return await _dbContext.Enclosures
            .Where(enclosure => enclosure.Id == enclosureId)
            .Select(enclosure => enclosure.Name)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static AnimalMovementActivityDto ToDto(ActivityEvent activityEvent, int animalId)
    {
        var animalAssociation = activityEvent.Animals.Single(association => association.AnimalId == animalId);
        return ToDto(
            activityEvent,
            animalId,
            animalAssociation.Animal.Name,
            activityEvent.AnimalMovement!.FromEnclosure.Name,
            activityEvent.AnimalMovement.ToEnclosure.Name);
    }

    private static AnimalMovementActivityDto ToDto(
        ActivityEvent activityEvent,
        int animalId,
        string animalName,
        string fromEnclosureName,
        string toEnclosureName)
    {
        var movement = activityEvent.AnimalMovement!;

        return new AnimalMovementActivityDto
        {
            ActivityEventId = activityEvent.Id,
            AnimalId = animalId,
            AnimalName = animalName,
            FromEnclosureId = movement.FromEnclosureId,
            FromEnclosureName = fromEnclosureName,
            ToEnclosureId = movement.ToEnclosureId,
            ToEnclosureName = toEnclosureName,
            OccurredAt = activityEvent.OccurredAt,
            Reason = movement.Reason,
            Notes = activityEvent.Notes,
            PerformedBy = activityEvent.PerformedBy,
            CreatedAt = activityEvent.CreatedAt,
            UpdatedAt = activityEvent.UpdatedAt,
        };
    }
}
