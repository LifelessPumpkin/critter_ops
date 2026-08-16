using Microsoft.EntityFrameworkCore;
using skipper_api.Data;
using skipper_api.Domain.Activity;
using skipper_api.Dtos.AnimalTreatments;

namespace skipper_api.Services.AnimalTreatments;

public class AnimalTreatmentActivityService : IAnimalTreatmentActivityService
{
    private readonly ProfessorDbContext _dbContext;

    public AnimalTreatmentActivityService(ProfessorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AnimalTreatmentActivityDto>?> GetByAnimalIdAsync(int animalId, CancellationToken cancellationToken = default)
    {
        var animalExists = await _dbContext.Animals.AsNoTracking().AnyAsync(animal => animal.Id == animalId, cancellationToken);
        if (!animalExists)
        {
            return null;
        }

        var events = await TreatmentEvents()
            .Where(activityEvent => activityEvent.Animals.Any(association => association.AnimalId == animalId))
            .OrderByDescending(activityEvent => activityEvent.OccurredAt)
            .ThenByDescending(activityEvent => activityEvent.Id)
            .ToListAsync(cancellationToken);

        return events.Select(activityEvent => ToDto(activityEvent, animalId)).ToList();
    }

    public async Task<AnimalTreatmentActivityDto?> GetByIdAsync(int animalId, long activityId, CancellationToken cancellationToken = default)
    {
        var treatmentEvent = await TreatmentEvents()
            .Where(activityEvent => activityEvent.Id == activityId &&
                activityEvent.Animals.Any(association => association.AnimalId == animalId))
            .SingleOrDefaultAsync(cancellationToken);

        return treatmentEvent is null ? null : ToDto(treatmentEvent, animalId);
    }

    public async Task<CreateAnimalTreatmentActivityResult> CreateAsync(
        int animalId,
        CreateAnimalTreatmentActivityDto request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var animal = await _dbContext.Animals
            .Include(animal => animal.Enclosure)
            .SingleOrDefaultAsync(animal => animal.Id == animalId, cancellationToken);

        if (animal is null)
        {
            return CreateAnimalTreatmentActivityResult.AnimalNotFound();
        }

        if (animal.Enclosure is null)
        {
            return CreateAnimalTreatmentActivityResult.EnclosureNotFound();
        }

        var now = DateTime.UtcNow;
        var activityEvent = new ActivityEvent
        {
            EventType = ActivityEventType.Treatment,
            OccurredAt = request.OccurredAt!.Value,
            Title = request.TreatmentName!,
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
                    EnclosureId = animal.EnclosureId,
                    RelationshipType = ActivityEventEnclosureRelationshipType.Primary,
                },
            ],
            AnimalTreatment = new AnimalTreatmentActivity
            {
                TreatmentType = request.TreatmentType,
                TreatmentName = request.TreatmentName!,
                Result = request.Result,
            },
        };

        _dbContext.ActivityEvents.Add(activityEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return CreateAnimalTreatmentActivityResult.Created(ToDto(activityEvent, animalId, animal.Name, animal.EnclosureId, animal.Enclosure.Name));
    }

    public async Task<UpdateAnimalTreatmentActivityResult> UpdateAsync(
        int animalId,
        long activityId,
        UpdateAnimalTreatmentActivityDto request,
        CancellationToken cancellationToken = default)
    {
        var treatmentEvent = await _dbContext.ActivityEvents
            .Include(activityEvent => activityEvent.AnimalTreatment)
            .Include(activityEvent => activityEvent.Animals)
                .ThenInclude(association => association.Animal)
            .Include(activityEvent => activityEvent.Enclosures)
                .ThenInclude(association => association.Enclosure)
            .SingleOrDefaultAsync(activityEvent => activityEvent.Id == activityId &&
                activityEvent.EventType == ActivityEventType.Treatment &&
                activityEvent.Animals.Any(association => association.AnimalId == animalId), cancellationToken);

        if (treatmentEvent?.AnimalTreatment is null)
        {
            return UpdateAnimalTreatmentActivityResult.NotFound();
        }

        treatmentEvent.OccurredAt = request.OccurredAt!.Value;
        treatmentEvent.Title = request.TreatmentName!;
        treatmentEvent.Notes = request.Notes;
        treatmentEvent.PerformedBy = request.PerformedBy;
        treatmentEvent.UpdatedAt = DateTime.UtcNow;
        treatmentEvent.AnimalTreatment.TreatmentType = request.TreatmentType;
        treatmentEvent.AnimalTreatment.TreatmentName = request.TreatmentName!;
        treatmentEvent.AnimalTreatment.Result = request.Result;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return UpdateAnimalTreatmentActivityResult.Updated(ToDto(treatmentEvent, animalId));
    }

    public async Task<DeleteAnimalTreatmentActivityResult> DeleteAsync(int animalId, long activityId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var treatmentEvent = await _dbContext.ActivityEvents
            .Include(activityEvent => activityEvent.AnimalTreatment)
            .Include(activityEvent => activityEvent.Animals)
            .SingleOrDefaultAsync(activityEvent => activityEvent.Id == activityId &&
                activityEvent.EventType == ActivityEventType.Treatment &&
                activityEvent.Animals.Any(association => association.AnimalId == animalId), cancellationToken);

        if (treatmentEvent?.AnimalTreatment is null)
        {
            return DeleteAnimalTreatmentActivityResult.NotFound;
        }

        _dbContext.ActivityEvents.Remove(treatmentEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return DeleteAnimalTreatmentActivityResult.Deleted;
    }

    private IQueryable<ActivityEvent> TreatmentEvents()
    {
        return _dbContext.ActivityEvents
            .AsNoTracking()
            .Include(activityEvent => activityEvent.AnimalTreatment)
            .Include(activityEvent => activityEvent.Animals)
                .ThenInclude(association => association.Animal)
            .Include(activityEvent => activityEvent.Enclosures)
                .ThenInclude(association => association.Enclosure)
            .Where(activityEvent => activityEvent.EventType == ActivityEventType.Treatment &&
                activityEvent.AnimalTreatment != null);
    }

    private static AnimalTreatmentActivityDto ToDto(ActivityEvent activityEvent, int animalId)
    {
        var animalAssociation = activityEvent.Animals.Single(association => association.AnimalId == animalId);
        var enclosureAssociation = activityEvent.Enclosures
            .OrderBy(association => association.RelationshipType == ActivityEventEnclosureRelationshipType.Primary ? 0 : 1)
            .First();

        return ToDto(activityEvent, animalId, animalAssociation.Animal.Name, enclosureAssociation.EnclosureId, enclosureAssociation.Enclosure.Name);
    }

    private static AnimalTreatmentActivityDto ToDto(
        ActivityEvent activityEvent,
        int animalId,
        string animalName,
        int enclosureId,
        string enclosureName)
    {
        var treatment = activityEvent.AnimalTreatment!;
        return new AnimalTreatmentActivityDto
        {
            ActivityEventId = activityEvent.Id,
            AnimalId = animalId,
            AnimalName = animalName,
            EnclosureId = enclosureId,
            EnclosureName = enclosureName,
            TreatmentType = treatment.TreatmentType,
            TreatmentName = treatment.TreatmentName,
            Result = treatment.Result,
            OccurredAt = activityEvent.OccurredAt,
            Notes = activityEvent.Notes,
            PerformedBy = activityEvent.PerformedBy,
            CreatedAt = activityEvent.CreatedAt,
            UpdatedAt = activityEvent.UpdatedAt,
        };
    }
}
