using Microsoft.EntityFrameworkCore;
using skipper_api.Data;
using skipper_api.Domain.Activity;
using skipper_api.Dtos.AnimalMedications;

namespace skipper_api.Services.AnimalMedications;

public class AnimalMedicationActivityService : IAnimalMedicationActivityService
{
    private readonly ProfessorDbContext _dbContext;

    public AnimalMedicationActivityService(ProfessorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AnimalMedicationActivityDto>?> GetByAnimalIdAsync(int animalId, CancellationToken cancellationToken = default)
    {
        var animalExists = await _dbContext.Animals.AsNoTracking().AnyAsync(animal => animal.Id == animalId, cancellationToken);
        if (!animalExists)
        {
            return null;
        }

        var events = await MedicationEvents()
            .Where(activityEvent => activityEvent.Animals.Any(association => association.AnimalId == animalId))
            .OrderByDescending(activityEvent => activityEvent.OccurredAt)
            .ThenByDescending(activityEvent => activityEvent.Id)
            .ToListAsync(cancellationToken);

        return events.Select(activityEvent => ToDto(activityEvent, animalId)).ToList();
    }

    public async Task<AnimalMedicationActivityDto?> GetByIdAsync(int animalId, long activityId, CancellationToken cancellationToken = default)
    {
        var medicationEvent = await MedicationEvents()
            .Where(activityEvent => activityEvent.Id == activityId &&
                activityEvent.Animals.Any(association => association.AnimalId == animalId))
            .SingleOrDefaultAsync(cancellationToken);

        return medicationEvent is null ? null : ToDto(medicationEvent, animalId);
    }

    public async Task<CreateAnimalMedicationActivityResult> CreateAsync(
        int animalId,
        CreateAnimalMedicationActivityDto request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var animal = await _dbContext.Animals
            .Include(animal => animal.Enclosure)
            .SingleOrDefaultAsync(animal => animal.Id == animalId, cancellationToken);

        if (animal is null)
        {
            return CreateAnimalMedicationActivityResult.AnimalNotFound();
        }

        if (animal.Enclosure is null)
        {
            return CreateAnimalMedicationActivityResult.EnclosureNotFound();
        }

        var now = DateTime.UtcNow;
        var activityEvent = new ActivityEvent
        {
            EventType = ActivityEventType.Medication,
            OccurredAt = request.OccurredAt!.Value,
            Title = ToAnimalTimelineTitle(request.MedicationName!, request.Dose!.Value, request.DoseUnit!, request.Route!.Value),
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
            AnimalMedication = new AnimalMedicationActivity
            {
                MedicationName = request.MedicationName!,
                Dose = request.Dose.Value,
                DoseUnit = request.DoseUnit!,
                Route = request.Route.Value,
                Result = request.Result,
            },
        };

        _dbContext.ActivityEvents.Add(activityEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return CreateAnimalMedicationActivityResult.Created(ToDto(activityEvent, animalId, animal.Name, animal.EnclosureId, animal.Enclosure.Name));
    }

    public async Task<UpdateAnimalMedicationActivityResult> UpdateAsync(
        int animalId,
        long activityId,
        UpdateAnimalMedicationActivityDto request,
        CancellationToken cancellationToken = default)
    {
        var medicationEvent = await _dbContext.ActivityEvents
            .Include(activityEvent => activityEvent.AnimalMedication)
            .Include(activityEvent => activityEvent.Animals)
                .ThenInclude(association => association.Animal)
            .Include(activityEvent => activityEvent.Enclosures)
                .ThenInclude(association => association.Enclosure)
            .SingleOrDefaultAsync(activityEvent => activityEvent.Id == activityId &&
                activityEvent.EventType == ActivityEventType.Medication &&
                activityEvent.Animals.Any(association => association.AnimalId == animalId), cancellationToken);

        if (medicationEvent?.AnimalMedication is null)
        {
            return UpdateAnimalMedicationActivityResult.NotFound();
        }

        medicationEvent.OccurredAt = request.OccurredAt!.Value;
        medicationEvent.Title = ToAnimalTimelineTitle(request.MedicationName!, request.Dose!.Value, request.DoseUnit!, request.Route!.Value);
        medicationEvent.Notes = request.Notes;
        medicationEvent.PerformedBy = request.PerformedBy;
        medicationEvent.UpdatedAt = DateTime.UtcNow;
        medicationEvent.AnimalMedication.MedicationName = request.MedicationName!;
        medicationEvent.AnimalMedication.Dose = request.Dose.Value;
        medicationEvent.AnimalMedication.DoseUnit = request.DoseUnit!;
        medicationEvent.AnimalMedication.Route = request.Route!.Value;
        medicationEvent.AnimalMedication.Result = request.Result;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return UpdateAnimalMedicationActivityResult.Updated(ToDto(medicationEvent, animalId));
    }

    public async Task<DeleteAnimalMedicationActivityResult> DeleteAsync(int animalId, long activityId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var medicationEvent = await _dbContext.ActivityEvents
            .Include(activityEvent => activityEvent.AnimalMedication)
            .Include(activityEvent => activityEvent.Animals)
            .SingleOrDefaultAsync(activityEvent => activityEvent.Id == activityId &&
                activityEvent.EventType == ActivityEventType.Medication &&
                activityEvent.Animals.Any(association => association.AnimalId == animalId), cancellationToken);

        if (medicationEvent?.AnimalMedication is null)
        {
            return DeleteAnimalMedicationActivityResult.NotFound;
        }

        _dbContext.ActivityEvents.Remove(medicationEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return DeleteAnimalMedicationActivityResult.Deleted;
    }

    private IQueryable<ActivityEvent> MedicationEvents()
    {
        return _dbContext.ActivityEvents
            .AsNoTracking()
            .Include(activityEvent => activityEvent.AnimalMedication)
            .Include(activityEvent => activityEvent.Animals)
                .ThenInclude(association => association.Animal)
            .Include(activityEvent => activityEvent.Enclosures)
                .ThenInclude(association => association.Enclosure)
            .Where(activityEvent => activityEvent.EventType == ActivityEventType.Medication &&
                activityEvent.AnimalMedication != null);
    }

    private static AnimalMedicationActivityDto ToDto(ActivityEvent activityEvent, int animalId)
    {
        var animalAssociation = activityEvent.Animals.Single(association => association.AnimalId == animalId);
        var enclosureAssociation = activityEvent.Enclosures
            .OrderBy(association => association.RelationshipType == ActivityEventEnclosureRelationshipType.Primary ? 0 : 1)
            .First();

        return ToDto(activityEvent, animalId, animalAssociation.Animal.Name, enclosureAssociation.EnclosureId, enclosureAssociation.Enclosure.Name);
    }

    private static AnimalMedicationActivityDto ToDto(
        ActivityEvent activityEvent,
        int animalId,
        string animalName,
        int enclosureId,
        string enclosureName)
    {
        var medication = activityEvent.AnimalMedication!;
        return new AnimalMedicationActivityDto
        {
            ActivityEventId = activityEvent.Id,
            AnimalId = animalId,
            AnimalName = animalName,
            EnclosureId = enclosureId,
            EnclosureName = enclosureName,
            MedicationName = medication.MedicationName,
            Dose = medication.Dose,
            DoseUnit = medication.DoseUnit,
            Route = medication.Route,
            Result = medication.Result,
            OccurredAt = activityEvent.OccurredAt,
            Notes = activityEvent.Notes,
            PerformedBy = activityEvent.PerformedBy,
            CreatedAt = activityEvent.CreatedAt,
            UpdatedAt = activityEvent.UpdatedAt,
        };
    }

    public static string ToAnimalTimelineTitle(string medicationName, decimal dose, string doseUnit, AnimalMedicationRoute route)
    {
        var formattedDose = dose % 1 == 0 ? decimal.Truncate(dose).ToString("0") : dose.ToString("0.####");
        return $"{medicationName} administered - {formattedDose} {doseUnit} {FormatRoute(route)}";
    }

    private static string FormatRoute(AnimalMedicationRoute route)
    {
        return route switch
        {
            AnimalMedicationRoute.Oral => "orally",
            AnimalMedicationRoute.Topical => "topically",
            AnimalMedicationRoute.Ophthalmic => "ophthalmic",
            AnimalMedicationRoute.Otic => "otic",
            AnimalMedicationRoute.Inhaled => "inhaled",
            _ => route.ToString(),
        };
    }
}
