using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using skipper_api.Data;
using skipper_api.Domain.Activity;
using skipper_api.Domain.EnclosureTimeline;
using skipper_api.Dtos.EnclosureTimeline;
using skipper_api.Services.EnclosureCleanings;

namespace skipper_api.Services.EnclosureTimeline;

public class EnclosureTimelineService : IEnclosureTimelineService
{
    private readonly ProfessorDbContext _dbContext;

    public EnclosureTimelineService(ProfessorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<EnclosureTimelineEventDto>> GetByEnclosureIdAsync(
        int enclosureId,
        CancellationToken cancellationToken = default)
    {
        var timelineEvents = await _dbContext.ActivityEvents
            .AsNoTracking()
            .Include(activityEvent => activityEvent.Enclosures)
                .ThenInclude(association => association.Enclosure)
            .Include(activityEvent => activityEvent.Animals)
                .ThenInclude(association => association.Animal)
            .Include(activityEvent => activityEvent.AnimalMovement)
                .ThenInclude(movement => movement!.FromEnclosure)
            .Include(activityEvent => activityEvent.AnimalMovement)
                .ThenInclude(movement => movement!.ToEnclosure)
            .Include(activityEvent => activityEvent.AnimalFeeding)
            .Include(activityEvent => activityEvent.AnimalDisposition)
            .Include(activityEvent => activityEvent.AnimalMedication)
            .Include(activityEvent => activityEvent.AnimalTreatment)
            .Include(activityEvent => activityEvent.EnclosureCleaning)
            .Where(activityEvent => activityEvent.Enclosures.Any(association => association.EnclosureId == enclosureId))
            .OrderByDescending(activityEvent => activityEvent.OccurredAt)
            .ThenByDescending(activityEvent => activityEvent.Id)
            .ToListAsync(cancellationToken);

        return timelineEvents.Select(activityEvent => ToDto(activityEvent, enclosureId)).ToList();
    }

    public async Task<EnclosureTimelineEventDto?> GetByIdAsync(
        int enclosureId,
        long eventId,
        CancellationToken cancellationToken = default)
    {
        var timelineEvent = await _dbContext.ActivityEvents
            .AsNoTracking()
            .Include(activityEvent => activityEvent.Enclosures)
                .ThenInclude(association => association.Enclosure)
            .Include(activityEvent => activityEvent.Animals)
                .ThenInclude(association => association.Animal)
            .Include(activityEvent => activityEvent.AnimalMovement)
                .ThenInclude(movement => movement!.FromEnclosure)
            .Include(activityEvent => activityEvent.AnimalMovement)
                .ThenInclude(movement => movement!.ToEnclosure)
            .Include(activityEvent => activityEvent.AnimalFeeding)
            .Include(activityEvent => activityEvent.AnimalDisposition)
            .Include(activityEvent => activityEvent.AnimalMedication)
            .Include(activityEvent => activityEvent.AnimalTreatment)
            .Include(activityEvent => activityEvent.EnclosureCleaning)
            .Where(activityEvent =>
                activityEvent.Id == eventId &&
                activityEvent.Enclosures.Any(association => association.EnclosureId == enclosureId))
            .SingleOrDefaultAsync(cancellationToken);

        return timelineEvent is null
            ? null
            : ToDto(timelineEvent, enclosureId);
    }

    public async Task<CreateTimelineEventResult> CreateAsync(
        int enclosureId,
        CreateEnclosureTimelineEventDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.EventType is EnclosureTimelineEventType.AnimalMovement
            or EnclosureTimelineEventType.Feeding
            or EnclosureTimelineEventType.AnimalDisposition
            or EnclosureTimelineEventType.Medication
            or EnclosureTimelineEventType.Treatment
            or EnclosureTimelineEventType.Cleaning)
        {
            return CreateTimelineEventResult.UnsupportedEventType();
        }

        var enclosureExists = await _dbContext.Enclosures
            .AsNoTracking()
            .AnyAsync(enclosure => enclosure.Id == enclosureId, cancellationToken);

        if (!enclosureExists)
        {
            return CreateTimelineEventResult.EnclosureNotFound();
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

        return CreateTimelineEventResult.Created(ToDto(timelineEvent, enclosureId));
    }

    public async Task<UpdateTimelineEventResult> UpdateAsync(
        int enclosureId,
        long eventId,
        UpdateEnclosureTimelineEventDto request,
        CancellationToken cancellationToken = default)
    {
        var timelineEvent = await _dbContext.ActivityEvents
            .SingleOrDefaultAsync(
                activityEvent =>
                    activityEvent.Id == eventId &&
                    activityEvent.Enclosures.Any(association => association.EnclosureId == enclosureId),
                cancellationToken);

        if (timelineEvent is null)
        {
            return UpdateTimelineEventResult.NotFound();
        }

        if (timelineEvent.EventType == ActivityEventType.AnimalMovement ||
            timelineEvent.EventType == ActivityEventType.Feeding ||
            timelineEvent.EventType == ActivityEventType.AnimalDisposition ||
            timelineEvent.EventType == ActivityEventType.Medication ||
            timelineEvent.EventType == ActivityEventType.Treatment ||
            timelineEvent.EventType == ActivityEventType.Cleaning ||
            request.EventType is EnclosureTimelineEventType.AnimalMovement
                or EnclosureTimelineEventType.Feeding
                or EnclosureTimelineEventType.AnimalDisposition
                or EnclosureTimelineEventType.Medication
                or EnclosureTimelineEventType.Treatment
                or EnclosureTimelineEventType.Cleaning)
        {
            return UpdateTimelineEventResult.UnsupportedEventType();
        }

        timelineEvent.EventType = ToActivityEventType(request.EventType!.Value);
        timelineEvent.OccurredAt = request.OccurredAt!.Value;
        timelineEvent.Title = request.Title!;
        timelineEvent.Notes = request.Description;
        timelineEvent.PerformedBy = request.PerformedBy;
        timelineEvent.Metadata = ToJsonDocument(request.Metadata);
        timelineEvent.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return UpdateTimelineEventResult.Updated(ToDto(timelineEvent, enclosureId));
    }

    public async Task<DeleteTimelineEventResult> DeleteAsync(
        int enclosureId,
        long eventId,
        CancellationToken cancellationToken = default)
    {
        var timelineEvent = await _dbContext.ActivityEvents
            .SingleOrDefaultAsync(
                activityEvent =>
                    activityEvent.Id == eventId &&
                    activityEvent.Enclosures.Any(association => association.EnclosureId == enclosureId),
                cancellationToken);

        if (timelineEvent is null)
        {
            return DeleteTimelineEventResult.NotFound;
        }

        if (timelineEvent.EventType is ActivityEventType.AnimalMovement
            or ActivityEventType.Feeding
            or ActivityEventType.AnimalDisposition
            or ActivityEventType.Medication
            or ActivityEventType.Treatment
            or ActivityEventType.Cleaning)
        {
            return DeleteTimelineEventResult.NotFound;
        }

        _dbContext.ActivityEvents.Remove(timelineEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return DeleteTimelineEventResult.Deleted;
    }

    private static JsonDocument? ToJsonDocument(JsonElement? metadata)
    {
        return metadata.HasValue
            ? JsonDocument.Parse(metadata.Value.GetRawText())
            : null;
    }

    private static EnclosureTimelineEventDto ToDto(ActivityEvent timelineEvent, int enclosureId)
    {
        return new EnclosureTimelineEventDto
        {
            Id = timelineEvent.Id,
            EnclosureId = enclosureId,
            EventType = ToEnclosureTimelineEventType(timelineEvent.EventType),
            OccurredAt = timelineEvent.OccurredAt,
            Title = ToTimelineTitle(timelineEvent, enclosureId),
            Description = timelineEvent.Notes,
            PerformedBy = timelineEvent.PerformedBy,
            SourceReferenceId = timelineEvent.SourceReferenceId,
            SourceType = timelineEvent.SourceType,
            Metadata = timelineEvent.Metadata?.RootElement.Clone(),
            CreatedAt = timelineEvent.CreatedAt,
            UpdatedAt = timelineEvent.UpdatedAt,
        };
    }

    private static ActivityEventType ToActivityEventType(EnclosureTimelineEventType eventType)
    {
        return eventType switch
        {
            EnclosureTimelineEventType.WaterTest => ActivityEventType.WaterTest,
            EnclosureTimelineEventType.Cleaning => ActivityEventType.Cleaning,
            EnclosureTimelineEventType.Feeding => ActivityEventType.Feeding,
            EnclosureTimelineEventType.AnimalMovement => ActivityEventType.AnimalMovement,
            EnclosureTimelineEventType.AnimalDisposition => ActivityEventType.AnimalDisposition,
            EnclosureTimelineEventType.Medication => ActivityEventType.Medication,
            EnclosureTimelineEventType.Treatment => ActivityEventType.Treatment,
            EnclosureTimelineEventType.Task => ActivityEventType.Task,
            EnclosureTimelineEventType.Other => ActivityEventType.Other,
            _ => ActivityEventType.Other,
        };
    }

    private static EnclosureTimelineEventType ToEnclosureTimelineEventType(ActivityEventType eventType)
    {
        return eventType switch
        {
            ActivityEventType.WaterTest => EnclosureTimelineEventType.WaterTest,
            ActivityEventType.Cleaning => EnclosureTimelineEventType.Cleaning,
            ActivityEventType.Feeding => EnclosureTimelineEventType.Feeding,
            ActivityEventType.AnimalMovement => EnclosureTimelineEventType.AnimalMovement,
            ActivityEventType.AnimalDisposition => EnclosureTimelineEventType.AnimalDisposition,
            ActivityEventType.Medication => EnclosureTimelineEventType.Medication,
            ActivityEventType.Treatment => EnclosureTimelineEventType.Treatment,
            ActivityEventType.Task => EnclosureTimelineEventType.Task,
            ActivityEventType.Other => EnclosureTimelineEventType.Other,
            _ => EnclosureTimelineEventType.Other,
        };
    }

    private static string ToTimelineTitle(ActivityEvent timelineEvent, int enclosureId)
    {
        if (timelineEvent.EventType != ActivityEventType.AnimalMovement ||
            timelineEvent.AnimalMovement is not { } movement)
        {
            return timelineEvent.EventType == ActivityEventType.Feeding &&
                timelineEvent.AnimalFeeding is { } feeding
                ? $"{GetAnimalName(timelineEvent)} was fed {FormatFeedingAmount(feeding.Quantity, feeding.Unit, feeding.Food)} - {feeding.Result}"
                : timelineEvent.EventType == ActivityEventType.AnimalDisposition &&
                    timelineEvent.AnimalDisposition is { } disposition
                    ? ToDispositionTitle(GetAnimalName(timelineEvent), disposition.DispositionType)
                    : timelineEvent.EventType is ActivityEventType.Medication or ActivityEventType.Treatment
                        ? $"{GetAnimalName(timelineEvent)} received medical treatment"
                        : timelineEvent.EventType == ActivityEventType.Cleaning &&
                            timelineEvent.EnclosureCleaning is { } cleaning
                            ? EnclosureCleaningActivityService.ToTimelineTitle(cleaning)
                            : timelineEvent.Title;
        }

        var animalName = GetAnimalName(timelineEvent);

        if (movement.FromEnclosureId == enclosureId)
        {
            return $"{animalName} left this enclosure for {movement.ToEnclosure.Name}";
        }

        if (movement.ToEnclosureId == enclosureId)
        {
            return $"{animalName} entered this enclosure from {movement.FromEnclosure.Name}";
        }

        return timelineEvent.Title;
    }

    private static string GetAnimalName(ActivityEvent timelineEvent)
    {
        return timelineEvent.Animals
            .OrderBy(association => association.RelationshipType == ActivityEventAnimalRelationshipType.Primary ? 0 : 1)
            .FirstOrDefault()
            ?.Animal
            .Name ?? "Animal";
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

    private static string ToDispositionTitle(string animalName, AnimalDispositionType dispositionType)
    {
        return dispositionType switch
        {
            AnimalDispositionType.Sold => $"{animalName} left active care - Sold",
            AnimalDispositionType.Surrendered => $"{animalName} left active care - Surrendered",
            AnimalDispositionType.Transferred => $"{animalName} was transferred out",
            AnimalDispositionType.Released => $"{animalName} was released",
            AnimalDispositionType.Deceased => $"{animalName} marked deceased",
            AnimalDispositionType.Other => $"{animalName} left active care",
            _ => $"{animalName} left active care",
        };
    }
}
