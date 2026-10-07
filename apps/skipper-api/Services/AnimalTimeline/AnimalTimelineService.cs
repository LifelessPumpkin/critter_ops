using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using skipper_api.Data;
using skipper_api.Domain.Activity;
using skipper_api.Domain.AnimalTimeline;
using skipper_api.Dtos.AnimalTimeline;

namespace skipper_api.Services.AnimalTimeline;

public class AnimalTimelineService : IAnimalTimelineService
{
    private readonly ProfessorDbContext _dbContext;

    public AnimalTimelineService(ProfessorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AnimalTimelineEventDto>?> GetByAnimalIdAsync(
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

        var timelineEvents = await _dbContext.ActivityEvents
            .AsNoTracking()
            .Include(activityEvent => activityEvent.Animals)
                .ThenInclude(association => association.Animal)
            .Include(activityEvent => activityEvent.Enclosures)
                .ThenInclude(association => association.Enclosure)
            .Include(activityEvent => activityEvent.AnimalMovement)
                .ThenInclude(movement => movement!.FromEnclosure)
            .Include(activityEvent => activityEvent.AnimalMovement)
                .ThenInclude(movement => movement!.ToEnclosure)
            .Include(activityEvent => activityEvent.AnimalFeeding)
            .Include(activityEvent => activityEvent.AnimalDisposition)
            .Include(activityEvent => activityEvent.AnimalMedication)
            .Include(activityEvent => activityEvent.AnimalTreatment)
            .Where(activityEvent => activityEvent.Animals.Any(association => association.AnimalId == animalId))
            .OrderByDescending(activityEvent => activityEvent.OccurredAt)
            .ThenByDescending(activityEvent => activityEvent.Id)
            .ToListAsync(cancellationToken);

        return timelineEvents.Select(activityEvent => ToDto(activityEvent, animalId)).ToList();
    }

    public async Task<AnimalTimelineEventDto?> GetByIdAsync(
        int animalId,
        long eventId,
        CancellationToken cancellationToken = default)
    {
        var timelineEvent = await _dbContext.ActivityEvents
            .AsNoTracking()
            .Include(activityEvent => activityEvent.Animals)
                .ThenInclude(association => association.Animal)
            .Include(activityEvent => activityEvent.Enclosures)
                .ThenInclude(association => association.Enclosure)
            .Include(activityEvent => activityEvent.AnimalMovement)
                .ThenInclude(movement => movement!.FromEnclosure)
            .Include(activityEvent => activityEvent.AnimalMovement)
                .ThenInclude(movement => movement!.ToEnclosure)
            .Include(activityEvent => activityEvent.AnimalFeeding)
            .Include(activityEvent => activityEvent.AnimalDisposition)
            .Include(activityEvent => activityEvent.AnimalMedication)
            .Include(activityEvent => activityEvent.AnimalTreatment)
            .Where(activityEvent =>
                activityEvent.Id == eventId &&
                activityEvent.Animals.Any(association => association.AnimalId == animalId))
            .SingleOrDefaultAsync(cancellationToken);

        return timelineEvent is null
            ? null
            : ToDto(timelineEvent, animalId);
    }

    public async Task<CreateAnimalTimelineEventResult> CreateAsync(
        int animalId,
        CreateAnimalTimelineEventDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.EventType is AnimalTimelineEventType.AnimalMovement
            or AnimalTimelineEventType.Feeding
            or AnimalTimelineEventType.AnimalDisposition
            or AnimalTimelineEventType.Medication
            or AnimalTimelineEventType.Treatment)
        {
            return CreateAnimalTimelineEventResult.UnsupportedEventType();
        }

        var animalName = await _dbContext.Animals
            .Where(animal => animal.Id == animalId)
            .Select(animal => animal.Name)
            .SingleOrDefaultAsync(cancellationToken);

        if (animalName is null)
        {
            return CreateAnimalTimelineEventResult.AnimalNotFound();
        }

        if (request.EnclosureId is not { } enclosureId)
        {
            return CreateAnimalTimelineEventResult.EnclosureNotFound();
        }

        var enclosureName = await _dbContext.Enclosures
            .Where(enclosure => enclosure.Id == enclosureId)
            .Select(enclosure => enclosure.Name)
            .SingleOrDefaultAsync(cancellationToken);

        if (enclosureName is null)
        {
            return CreateAnimalTimelineEventResult.EnclosureNotFound();
        }

        var now = DateTime.UtcNow;
        var timelineEvent = new ActivityEvent
        {
            EventType = ToActivityEventType(request.EventType!.Value),
            OccurredAt = request.OccurredAt!.Value,
            Title = request.Title!,
            Notes = request.Description,
            PerformedBy = request.PerformedBy,
            SourceReferenceId = request.SourceReferenceId,
            SourceType = request.SourceType,
            Metadata = ToJsonDocument(request.Metadata),
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
        };

        _dbContext.ActivityEvents.Add(timelineEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateAnimalTimelineEventResult.Created(ToDto(timelineEvent, animalId, animalName, enclosureId, enclosureName));
    }

    public async Task<UpdateAnimalTimelineEventResult> UpdateAsync(
        int animalId,
        long eventId,
        UpdateAnimalTimelineEventDto request,
        CancellationToken cancellationToken = default)
    {
        var timelineEvent = await _dbContext.ActivityEvents
            .Include(activityEvent => activityEvent.Animals)
            .Include(activityEvent => activityEvent.Enclosures)
            .SingleOrDefaultAsync(
                activityEvent =>
                    activityEvent.Id == eventId &&
                    activityEvent.Animals.Any(association => association.AnimalId == animalId),
                cancellationToken);

        if (timelineEvent is null)
        {
            return UpdateAnimalTimelineEventResult.NotFound();
        }

        if (timelineEvent.EventType == ActivityEventType.AnimalMovement ||
            timelineEvent.EventType == ActivityEventType.Feeding ||
            timelineEvent.EventType == ActivityEventType.AnimalDisposition ||
            timelineEvent.EventType == ActivityEventType.Medication ||
            timelineEvent.EventType == ActivityEventType.Treatment ||
            request.EventType is AnimalTimelineEventType.AnimalMovement
                or AnimalTimelineEventType.Feeding
                or AnimalTimelineEventType.AnimalDisposition
                or AnimalTimelineEventType.Medication
                or AnimalTimelineEventType.Treatment)
        {
            return UpdateAnimalTimelineEventResult.UnsupportedEventType();
        }

        if (request.EnclosureId is not { } enclosureId)
        {
            return UpdateAnimalTimelineEventResult.EnclosureNotFound();
        }

        var enclosureName = await _dbContext.Enclosures
            .Where(enclosure => enclosure.Id == enclosureId)
            .Select(enclosure => enclosure.Name)
            .SingleOrDefaultAsync(cancellationToken);

        if (enclosureName is null)
        {
            return UpdateAnimalTimelineEventResult.EnclosureNotFound();
        }

        timelineEvent.EventType = ToActivityEventType(request.EventType!.Value);
        timelineEvent.OccurredAt = request.OccurredAt!.Value;
        timelineEvent.Title = request.Title!;
        timelineEvent.Notes = request.Description;
        timelineEvent.PerformedBy = request.PerformedBy;
        timelineEvent.Metadata = ToJsonDocument(request.Metadata);
        timelineEvent.UpdatedAt = DateTime.UtcNow;

        timelineEvent.Enclosures.Clear();
        timelineEvent.Enclosures.Add(new ActivityEventEnclosure
        {
            ActivityEventId = timelineEvent.Id,
            EnclosureId = enclosureId,
            RelationshipType = ActivityEventEnclosureRelationshipType.Primary,
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        var animalName = await _dbContext.Animals
            .Where(animal => animal.Id == animalId)
            .Select(animal => animal.Name)
            .SingleAsync(cancellationToken);

        return UpdateAnimalTimelineEventResult.Updated(ToDto(timelineEvent, animalId, animalName, enclosureId, enclosureName));
    }

    public async Task<DeleteAnimalTimelineEventResult> DeleteAsync(
        int animalId,
        long eventId,
        CancellationToken cancellationToken = default)
    {
        var timelineEvent = await _dbContext.ActivityEvents
            .Include(activityEvent => activityEvent.Animals)
            .SingleOrDefaultAsync(
                activityEvent =>
                    activityEvent.Id == eventId &&
                    activityEvent.Animals.Any(association => association.AnimalId == animalId),
                cancellationToken);

        if (timelineEvent is null)
        {
            return DeleteAnimalTimelineEventResult.NotFound;
        }

        if (timelineEvent.EventType is ActivityEventType.AnimalMovement
            or ActivityEventType.Feeding
            or ActivityEventType.AnimalDisposition
            or ActivityEventType.Medication
            or ActivityEventType.Treatment)
        {
            return DeleteAnimalTimelineEventResult.NotFound;
        }

        _dbContext.ActivityEvents.Remove(timelineEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return DeleteAnimalTimelineEventResult.Deleted;
    }

    private static JsonDocument? ToJsonDocument(JsonElement? metadata)
    {
        return metadata.HasValue
            ? JsonDocument.Parse(metadata.Value.GetRawText())
            : null;
    }

    private static AnimalTimelineEventDto ToDto(ActivityEvent timelineEvent, int animalId)
    {
        var animalAssociation = timelineEvent.Animals.Single(association => association.AnimalId == animalId);
        var enclosureAssociation = timelineEvent.Enclosures
            .OrderBy(association => association.RelationshipType == ActivityEventEnclosureRelationshipType.Primary ? 0 : 1)
            .First();

        return ToDto(
            timelineEvent,
            animalAssociation.AnimalId,
            animalAssociation.Animal.Name,
            enclosureAssociation.EnclosureId,
            enclosureAssociation.Enclosure.Name);
    }

    private static AnimalTimelineEventDto ToDto(
        ActivityEvent timelineEvent,
        int animalId,
        string animalName,
        int enclosureId,
        string enclosureName)
    {
        return new AnimalTimelineEventDto
        {
            Id = timelineEvent.Id,
            AnimalId = animalId,
            AnimalName = animalName,
            EnclosureId = enclosureId,
            EnclosureName = enclosureName,
            EventType = ToAnimalTimelineEventType(timelineEvent.EventType),
            OccurredAt = timelineEvent.OccurredAt,
            Title = ToTimelineTitle(timelineEvent),
            Description = timelineEvent.Notes,
            PerformedBy = timelineEvent.PerformedBy,
            SourceReferenceId = timelineEvent.SourceReferenceId,
            SourceType = timelineEvent.SourceType,
            Metadata = timelineEvent.Metadata?.RootElement.Clone(),
            CreatedAt = timelineEvent.CreatedAt,
            UpdatedAt = timelineEvent.UpdatedAt,
        };
    }

    private static ActivityEventType ToActivityEventType(AnimalTimelineEventType eventType)
    {
        return eventType switch
        {
            AnimalTimelineEventType.Feeding => ActivityEventType.Feeding,
            AnimalTimelineEventType.AnimalMovement => ActivityEventType.AnimalMovement,
            AnimalTimelineEventType.AnimalDisposition => ActivityEventType.AnimalDisposition,
            AnimalTimelineEventType.Medication => ActivityEventType.Medication,
            AnimalTimelineEventType.Treatment => ActivityEventType.Treatment,
            AnimalTimelineEventType.Note => ActivityEventType.Note,
            AnimalTimelineEventType.Task => ActivityEventType.Task,
            AnimalTimelineEventType.Other => ActivityEventType.Other,
            _ => ActivityEventType.Other,
        };
    }

    private static AnimalTimelineEventType ToAnimalTimelineEventType(ActivityEventType eventType)
    {
        return eventType switch
        {
            ActivityEventType.Feeding => AnimalTimelineEventType.Feeding,
            ActivityEventType.AnimalMovement => AnimalTimelineEventType.AnimalMovement,
            ActivityEventType.AnimalDisposition => AnimalTimelineEventType.AnimalDisposition,
            ActivityEventType.Medication => AnimalTimelineEventType.Medication,
            ActivityEventType.Treatment => AnimalTimelineEventType.Treatment,
            ActivityEventType.Note => AnimalTimelineEventType.Note,
            ActivityEventType.Task => AnimalTimelineEventType.Task,
            ActivityEventType.Other => AnimalTimelineEventType.Other,
            _ => AnimalTimelineEventType.Other,
        };
    }

    private static string ToTimelineTitle(ActivityEvent timelineEvent)
    {
        return timelineEvent.EventType == ActivityEventType.AnimalMovement &&
            timelineEvent.AnimalMovement is { } movement
            ? $"Moved from {movement.FromEnclosure.Name} to {movement.ToEnclosure.Name}"
            : timelineEvent.EventType == ActivityEventType.Feeding &&
                timelineEvent.AnimalFeeding is { } feeding
                ? $"Fed {FormatFeedingAmount(feeding.Quantity, feeding.Unit, feeding.Food)} - {feeding.Result}"
                : timelineEvent.EventType == ActivityEventType.AnimalDisposition &&
                    timelineEvent.AnimalDisposition is { } disposition
                    ? ToDispositionTitle(disposition.DispositionType, disposition.RecipientOrDestination)
                    : timelineEvent.EventType == ActivityEventType.Medication &&
                        timelineEvent.AnimalMedication is { } medication
                        ? ToMedicationTitle(medication)
                        : timelineEvent.EventType == ActivityEventType.Treatment &&
                            timelineEvent.AnimalTreatment is { } treatment
                            ? treatment.TreatmentName
                            : timelineEvent.Title;
    }

    private static string FormatFeedingAmount(decimal quantity, AnimalFeedingQuantityUnit unit, string food)
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

    private static string ToDispositionTitle(
        AnimalDispositionType dispositionType,
        string? recipientOrDestination)
    {
        return dispositionType switch
        {
            AnimalDispositionType.Sold when !string.IsNullOrWhiteSpace(recipientOrDestination) =>
                $"Sold to {recipientOrDestination}",
            AnimalDispositionType.Surrendered when !string.IsNullOrWhiteSpace(recipientOrDestination) =>
                $"Surrendered to {recipientOrDestination}",
            AnimalDispositionType.Transferred when !string.IsNullOrWhiteSpace(recipientOrDestination) =>
                $"Transferred to {recipientOrDestination}",
            AnimalDispositionType.Released => "Released",
            AnimalDispositionType.Deceased => "Marked deceased",
            AnimalDispositionType.Other when !string.IsNullOrWhiteSpace(recipientOrDestination) =>
                $"Disposition recorded for {recipientOrDestination}",
            _ => dispositionType.ToString(),
        };
    }

    private static string ToMedicationTitle(AnimalMedicationActivity medication)
    {
        var formattedDose = medication.Dose % 1 == 0
            ? decimal.Truncate(medication.Dose).ToString("0")
            : medication.Dose.ToString("0.####");

        return $"{medication.MedicationName} administered - {formattedDose} {medication.DoseUnit} {FormatRoute(medication.Route)}";
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
